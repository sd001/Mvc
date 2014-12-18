﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.DependencyInjection.ServiceLookup;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Mvc
{
    public abstract class RazorPreCompileModule : ICompileModule
    {
        private string _file;
        private readonly IServiceProvider _appServices;

        public RazorPreCompileModule(IServiceProvider services)
        {
            _appServices = services;
        }

        protected virtual string FileExtension { get; } = ".cshtml";

        public virtual void BeforeCompile(IBeforeCompileContext context)
        {
            var appEnv = _appServices.GetRequiredService<IApplicationEnvironment>();

            _file = Path.Combine(@"d:\temp\RazorPreCompile");
            Directory.CreateDirectory(_file);
            _file = Path.Combine(_file, appEnv.ApplicationName + ".txt");
            var setup = new RazorViewEngineOptionsSetup(appEnv);
            var sc = new ServiceCollection();
            sc.ConfigureOptions(setup);
            sc.AddMvc();

            var sw = Stopwatch.StartNew();
            Log("[" + appEnv.RuntimeFramework + "]: Before RazorPreCompiler", sw);
            var cache = _appServices.GetRequiredService<ICompilationCache>();
            Log("[" + cache.GetHashCode() + "]", sw);
            var viewCompiler = new RazorPreCompiler(BuildFallbackServiceProvider(sc, _appServices));
            viewCompiler.CompileViews(context);
            Log("[" + appEnv.RuntimeFramework + "]: After RazorPreCompiler", sw);
            File.AppendAllText(_file, Environment.NewLine + Environment.NewLine);
        }

        private void Log(string v, Stopwatch sw)
        {
            File.AppendAllText(_file, sw.Elapsed.TotalMilliseconds + " " + v + Environment.NewLine);
        }


        public void AfterCompile(IAfterCompileContext context)
        {
        }

        // TODO: KILL THIS
        private static IServiceProvider BuildFallbackServiceProvider(IEnumerable<IServiceDescriptor> services, IServiceProvider fallback)
        {
            var sc = HostingServices.Create(fallback);
            sc.Add(services);

            // Build the manifest
            var manifestTypes = services.Where(t => t.ServiceType.GetTypeInfo().GenericTypeParameters.Length == 0
                    && t.ServiceType != typeof(IServiceManifest)
                    && t.ServiceType != typeof(IServiceProvider))
                    .Select(t => t.ServiceType).Distinct();
            sc.AddInstance<IServiceManifest>(new ServiceManifest(manifestTypes, fallback.GetRequiredService<IServiceManifest>()));
            return sc.BuildServiceProvider();
        }

        private class ServiceManifest : IServiceManifest
        {
            public ServiceManifest(IEnumerable<Type> services, IServiceManifest fallback = null)
            {
                Services = services;
                if (fallback != null)
                {
                    Services = Services.Concat(fallback.Services).Distinct();
                }
            }

            public IEnumerable<Type> Services { get; private set; }
        }
    }
}

namespace Microsoft.Framework.Runtime
{
    [AssemblyNeutral]
    public interface ICompileModule
    {
        void BeforeCompile(IBeforeCompileContext context);

        void AfterCompile(IAfterCompileContext context);
    }

    [AssemblyNeutral]
    public interface IAfterCompileContext
    {
        CSharpCompilation CSharpCompilation { get; set; }

        IList<Diagnostic> Diagnostics { get; }
    }
}
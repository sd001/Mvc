﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Generator.Compiler;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor.Directives
{
    public class ChunkInheritanceUtilityTest
    {
        [Fact]
        public void GetInheritedChunks_ReadsChunksFromViewStartsInPath()
        {
            // Arrange
            var fileSystem = new TestFileSystem();
            fileSystem.AddFile(@"Views\accounts\_ViewStart.cshtml", "@using AccountModels");
            fileSystem.AddFile(@"Views\Shared\_ViewStart.cshtml", "@inject SharedHelper Shared");
            fileSystem.AddFile(@"Views\home\_ViewStart.cshtml", "@using MyNamespace");
            fileSystem.AddFile(@"Views\_ViewStart.cshtml",
@"@inject MyHelper<TModel> Helper
@inherits MyBaseType

@{
    Layout = ""test.cshtml"";
}

");
            var defaultChunks = new Chunk[]
            {
                new InjectChunk("MyTestHtmlHelper", "Html"),
                new UsingChunk { Namespace = "AppNamespace.Model" },
            };
            var host = new MvcRazorHost(fileSystem);
            var utility = new ChunkInheritanceUtility(host, fileSystem, defaultChunks);

            // Act
            var codeTrees = utility.GetInheritedCodeTrees(@"Views\home\Index.cshtml");

            // Assert
            Assert.Equal(2, codeTrees.Count);
            var viewStartChunks = codeTrees[0].Chunks;
            Assert.Equal(3, viewStartChunks.Count);

            Assert.IsType<LiteralChunk>(viewStartChunks[0]);
            var usingChunk = Assert.IsType<UsingChunk>(viewStartChunks[1]);
            Assert.Equal("MyNamespace", usingChunk.Namespace);
            Assert.IsType<LiteralChunk>(viewStartChunks[2]);

            viewStartChunks = codeTrees[1].Chunks;
            Assert.Equal(5, viewStartChunks.Count);

            Assert.IsType<LiteralChunk>(viewStartChunks[0]);

            var injectChunk = Assert.IsType<InjectChunk>(viewStartChunks[1]);
            Assert.Equal("MyHelper<TModel>", injectChunk.TypeName);
            Assert.Equal("Helper", injectChunk.MemberName);

            var setBaseTypeChunk = Assert.IsType<SetBaseTypeChunk>(viewStartChunks[2]);
            Assert.Equal("MyBaseType", setBaseTypeChunk.TypeName);

            Assert.IsType<StatementChunk>(viewStartChunks[3]);
            Assert.IsType<LiteralChunk>(viewStartChunks[4]);
        }

        [Fact]
        public void GetInheritedChunks_ReturnsEmptySequenceIfNoViewStartsArePresent()
        {
            // Arrange
            var fileSystem = new TestFileSystem();
            fileSystem.AddFile(@"_ViewStart.cs", string.Empty);
            fileSystem.AddFile(@"Views\_Layout.cshtml", string.Empty);
            fileSystem.AddFile(@"Views\home\_not-viewstart.cshtml", string.Empty);
            var host = new MvcRazorHost(fileSystem);
            var defaultChunks = new Chunk[]
            {
                new InjectChunk("MyTestHtmlHelper", "Html"),
                new UsingChunk { Namespace = "AppNamespace.Model" },
            };
            var utility = new ChunkInheritanceUtility(host, fileSystem, defaultChunks);

            // Act
            var codeTrees = utility.GetInheritedCodeTrees(@"Views\home\Index.cshtml");

            // Assert
            Assert.Empty(codeTrees);
        }

        [Fact]
        public void MergeInheritedChunks_MergesDefaultInheritedChunks()
        {
            // Arrange
            var fileSystem = new TestFileSystem();
            fileSystem.AddFile(@"Views\_ViewStart.cshtml",
                               "@inject DifferentHelper<TModel> Html");
            var host = new MvcRazorHost(fileSystem);
            var defaultChunks = new Chunk[]
            {
                new InjectChunk("MyTestHtmlHelper", "Html"),
                new UsingChunk { Namespace = "AppNamespace.Model" },
            };
            var inheritedCodeTrees = new CodeTree[]
            {
                new CodeTree
                {
                    Chunks = new Chunk[]
                    {
                        new UsingChunk { Namespace = "InheritedNamespace" },
                        new LiteralChunk { Text = "some text" }
                    }
                },
                new CodeTree
                {
                    Chunks = new Chunk[]
                    {
                        new UsingChunk { Namespace = "AppNamespace.Model" },
                    }
                }
            };

            var utility = new ChunkInheritanceUtility(host, fileSystem, defaultChunks);
            var codeTree = new CodeTree();

            // Act
            utility.MergeInheritedCodeTrees(codeTree,
                                            inheritedCodeTrees,
                                            "dynamic");

            // Assert
            Assert.Equal(3, codeTree.Chunks.Count);
            Assert.Same(inheritedCodeTrees[0].Chunks[0], codeTree.Chunks[0]);
            Assert.Same(inheritedCodeTrees[1].Chunks[0], codeTree.Chunks[1]);
            Assert.Same(defaultChunks[0], codeTree.Chunks[2]);
        }
    }
}
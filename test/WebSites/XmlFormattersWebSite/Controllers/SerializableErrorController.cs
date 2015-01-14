// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc;

namespace XmlFormattersWebSite.Controllers
{
    public class SerializableErrorController : Controller
    {
        [HttpGet]
        public IActionResult ModelStateErrors()
        {
            Exception exception = null;

            try
            {
                throw new Exception("Error in executing the action");
            }
            catch(Exception ex)
            {
                exception = ex;
            }

            ModelState.AddModelError("key1", "key1-error");
            ModelState.AddModelError("key2", exception);

            return new ObjectResult(new SerializableError(ModelState));
        }

        [HttpPost]
        public IActionResult LogErrors([FromBody] SerializableError serializableError)
        {
            if(!ModelState.IsValid)
            {
                return HttpBadRequest(ModelState);
            }

            return new ObjectResult(serializableError);
        }
    }
}

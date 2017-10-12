using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lemuria.Core.Server.Controllers
{
    [Controller]
    public abstract class ApiController
    {
        [ActionContext]
        public ActionContext ActionContext { get; set; }
    }
}

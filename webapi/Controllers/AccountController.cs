using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace UnderstandingWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        [HttpGet]
        public string Greet()
        {
            return "Hello World!!";
        }

        [HttpGet("GreetWithName")]
        public string Greet(string name)
        {
            return $"Hello {name}!!";
        }
        [HttpPost]
        public string GreetPost(Account account)
        {
            return $"Hello {account.Name}!!";
        }
    }
    public class Account
    {
        [Required]
        public string Name { get; set; }
        public string Email { get; set; }
    }
}


using apitest.Services;
using Dapper;
using GotsThorlabs.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using System.IdentityModel.Tokens.Jwt;

namespace apitest.Controllers
{
    [Route("[Controller]")]
    [ApiController]
    public class InnerloginController : ControllerBase
    {

        
        //[Authorize(Policy = "administrator")]
        [Authorize]
        [HttpPut]
        public IActionResult Put() { return Ok("hola entro"); }
        [HttpPost]
        public IActionResult Post(login sesionuser)
        {
            var directorio = Directory.GetCurrentDirectory();

            
            using (var queryable = ConnectionSqlite.CreateConnection())
            {

                ///queryable.Open();
                //string loginString = "SELECT * FROM usertesting WHERE (name = @User OR email = @user) AND contrasena = @Password";
                string loginString = "SELECT * FROM user WHERE (nickname = @User OR correo =  @User) AND password =  @Password";
                var tableUsers =  queryable.Query<User>(loginString, sesionuser);

                if (tableUsers.Count() > 0)
                {
                    var token = TokenGenerator.GenerateTokenJwt(tableUsers.FirstOrDefault().IdUser.ToString());
                    return Ok(token);
                } else
                {
                    return BadRequest(new errorresponse { Error = "Fallo en la validacion", Description = "intente de nuevo iniciar session" });
                }
            }
        }
        [HttpGet]
        public IActionResult Getdata()//el iactionresult resulta la clase mas manejable falta ver la variante con "<>" la final
        {
            
            var stream = HttpContext.Request.Headers.Authorization.ToString();
            var handler = new JwtSecurityTokenHandler();
            var stream2 = stream.Remove(0,7);
            var jsonToken = handler.ReadToken(stream2);
            var tokenS = jsonToken as JwtSecurityToken;
            var GetToUser = tokenS.Payload["unique_name"];
            string token="sepudo verificar ";
            using (var queryable = ConnectionSqlite.CreateConnection())
            {
                queryable.Open();
                string loginString = "SELECT * FROM user WHERE idUser = @userId";
                var rowsAffected = queryable.Query(loginString, new { userId = GetToUser }).ToList();
                //respuesta = rowsAffected.Count() > 0 ? rowsAffected : respuesta.ToList();
                return Ok(rowsAffected);
               
            }
            //return BadRequest(new errorresponse { Error = "Fallo en la validacion", Description = "intente de nuevo iniciar session" } );
        }
    }

    public class login { 
    
        public string User { get; set; }
        public string Password { get; set; }
    }

    public class User
    {

        public int  IdUser { get; set; }
        public string correo { get; set; }
        public string nickname { get; set; }
    }
    public class errorresponse { 
        public string Error { get; set; }

        public string Description { get; set; }
    }
}

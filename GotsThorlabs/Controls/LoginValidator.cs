using apitest.Services;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace apitest.Controllers
{
    [ApiController]
    [Route("[Controller]")]
    public class InnerloginController : ControllerBase
    {

        [HttpPut]
        //[Authorize(Policy = "administrator")]
        [Authorize]
        public IActionResult Put() { return Ok("hola entro"); }
        [HttpPost]
        public IActionResult Postdata(login sesionuser)
        {
            //var nuevovalor = otherData.ToString();
            var respuesta = new List<dynamic>();
            var conectionable = new ConnectionSql();
            using (var queryable = conectionable.CreateConnection())
            {

                queryable.Open();
                string loginString = "SELECT * FROM usertesting WHERE (name = @User OR email = @user) AND contrasena = @Password";
                var rowsAffected = queryable.Query(loginString, sesionuser).ToList();
                 respuesta = rowsAffected.Count() > 0 ? rowsAffected : respuesta.ToList();
                if (respuesta.Count > 0)
                {
                    var token = TokenGenerator.GenerateTokenJwt(rowsAffected[0].userId.ToString());
                    return Ok(token);
                } else
                {
                    return BadRequest(new errorresponse { Error = "Fallo en la validacion", Description = "intente de nuevo iniciar session" });
                }
            }
        }
        public IActionResult Getdata()//el iactionresult resulta la clase mas manejable falta ver la variante con "<>" la final
        {
            
            var stream = HttpContext.Request.Headers.Authorization.ToString();
            var handler = new JwtSecurityTokenHandler();
            var stream2 = stream.Remove(0,7);
            var jsonToken = handler.ReadToken(stream2);
            var tokenS = jsonToken as JwtSecurityToken;
            var GetToUser = tokenS.Payload["unique_name"];
            string token="sepudo verificar ";
            var conectionable = new ConnectionSql();
            using (var queryable = conectionable.CreateConnection())
            {
                queryable.Open();
                string loginString = "SELECT * FROM usertesting WHERE Userid = @userId";
                var rowsAffected = queryable.Query(loginString, new { userId = GetToUser }).ToList();
                //respuesta = rowsAffected.Count() > 0 ? rowsAffected : respuesta.ToList();
                return Ok(rowsAffected);
               
            }
        }
    }

    public class login { 
    
        public string User { get; set; }
        public string Password { get; set; }
    }

    public class errorresponse { 
        public string Error { get; set; }

        public string Description { get; set; }
    }
}

using Microsoft.AspNetCore.Mvc;

namespace Etax_Api.Class.EtaxValidator.Tripetch
{
    public static class TripetchEtaxResponseHelper
    {
        public static IActionResult BadRequest(string code, string message, string msgId)
        {
            return new ObjectResult(new { error_code = code, message = $"MsgErrorID : {msgId} | {message}" })
            {
                StatusCode = 400
            };
        }
    }
}

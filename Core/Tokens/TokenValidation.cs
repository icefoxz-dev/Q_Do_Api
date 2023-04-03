using System.Security.Claims;

namespace OrderApiFun.Core.Tokens
{
    public struct TokenValidation
    {
        public enum Results
        {
            Valid,
            Expired,
            Error
        }
        public Results Result { get; set; }
        public string ErrorMessage { get; set; }
        public ClaimsPrincipal Principal { get; set; }

        public static TokenValidation Valid(ClaimsPrincipal principal) =>
            new() { Result = Results.Valid, Principal = principal };
        public static TokenValidation Error(string errorMessage) => new() { Result = Results.Error, ErrorMessage = errorMessage };
        public static TokenValidation Expired() => new() { Result = Results.Expired, ErrorMessage = "Token Expired!" };
    }
}

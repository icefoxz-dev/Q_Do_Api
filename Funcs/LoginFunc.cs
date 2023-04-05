using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OrderApiFun.Core.Middlewares;
using OrderApiFun.Core.Services;
using OrderDbLib.Entities;
using OrderLib;

namespace OrderApiFun.Funcs
{
    public class LoginFunc
    {
        private JwtTokenService JwtService { get; }
        private UserManager<User> UserManager { get; }
        public LoginFunc(JwtTokenService jwtService, UserManager<User> userManager)
        {
            JwtService = jwtService;
            UserManager = userManager;
        }

        public class RegisterModel
        {
            public string Username { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
        }
        
        [Function(nameof(Anonymous_RegisterApi))]
        public async Task<HttpResponseData> Anonymous_RegisterApi(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            HttpRequestData req,
            FunctionContext context)
        {
            var log = context.GetLogger(nameof(Anonymous_RegisterApi));
            var data = await req.ReadFromJsonAsync<RegisterModel>();

            var user = new User { UserName = data.Username, Email = data.Email };
            var result = await UserManager.CreateAsync(user, data.Password);

            if (!result.Succeeded)
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                errorResponse.WriteString(string.Join("\n",
                    result.Errors.Select(r => $"{r.Code}:{r.Description}")));
                return errorResponse;
            }

            var token = JwtService.GenerateAccessToken(user);
            var successResponse = req.CreateResponse(HttpStatusCode.OK);
            successResponse.WriteString(token);

            return successResponse;
        }

        public class LoginModel
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }
        [Function(nameof(Anonymous_LoginApi))]
        public async Task<HttpResponseData> Anonymous_LoginApi(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
            FunctionContext context)
        {
            var log = context.GetLogger(nameof(Anonymous_LoginApi));

            var loginModel = await req.ReadFromJsonAsync<LoginModel>();

            var user = await UserManager.FindByNameAsync(loginModel.Username);
            if (user == null)
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                errorResponse.WriteString("Invalid username or password.");
                return errorResponse;
            }

            var isValidPassword = await UserManager.CheckPasswordAsync(user, loginModel.Password);
            if (!isValidPassword)
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                errorResponse.WriteString("Invalid username or password.");
                return errorResponse;
            }

            var token = JwtService.GenerateAccessToken(user);
            var refreshToken = JwtService.GenerateRefreshToken(user);
            var successResponse = req.CreateResponse(HttpStatusCode.OK);
            await successResponse.WriteAsJsonAsync(new
            {
                Token = token,
                Refresh = refreshToken
            });

            return successResponse;
        }
    

        [Function(nameof(User_ReloginApi))]
        public async Task<HttpResponseData> User_ReloginApi(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
            FunctionContext context)
        {
            var log = context.GetLogger(nameof(User_ReloginApi));
            var refreshToken = GetValueFromHeader(req, JwtTokenService.RefreshTokenHeader);

            var hasRefreshToken = !string.IsNullOrWhiteSpace(refreshToken);

            if (!hasRefreshToken)
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                if (!hasRefreshToken) await badRequestResponse.WriteStringAsync("Refresh token is missing");
                return badRequestResponse;
            }

            var model = await req.ReadFromJsonAsync<RefreshTokenModel>();
            if (model == null)
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                if (!hasRefreshToken) await badRequestResponse.WriteStringAsync("Username not found!");
                return badRequestResponse;
            }

            var isValid = await JwtService.ValidateRefreshTokenAsync(refreshToken, model.Username);
            if(!isValid)
            {
                log.LogError("Invalid refresh token");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Invalid refresh token");
                return badRequestResponse;
            }

            var user = await UserManager.FindByNameAsync(model.Username);
            if (user == null)
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("User not found");
                return badRequestResponse;
            }

            var newAccessToken = JwtService.GenerateAccessToken(user);

            var okResponse = req.CreateResponse(HttpStatusCode.OK);
            okResponse.Headers.Add("Content-Type", "application/json");
            await okResponse.WriteStringAsync(JsonConvert.SerializeObject(new { access_token = newAccessToken }));
            return okResponse;

        }
        private class RefreshTokenModel
        {
            public string Username { get; set; }
        }

        private string GetValueFromHeader(HttpRequestData req,string header)
        {
            req.Headers.TryGetValues(header, out var tokenValues);
            var tokenArray = tokenValues?.ToArray() ?? null;
            var refreshToken = (tokenArray?.FirstOrDefault() ?? null) ?? string.Empty;
            return refreshToken;
        }

        [Function(nameof(User_TestApi))]
        public async Task<HttpResponseData> User_TestApi(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req, FunctionContext context)
        {
            // 检查是否存在 HttpResponseData 对象
            if (context.Items.ContainsKey("HttpResponseData"))
            {
                // 如果存在，表示验证失败，直接返回这个 HttpResponseData 对象
                return (HttpResponseData)context.Items["HttpResponseData"];
            }

            var userId = context.Items[Auth.UserId].ToString();
            var user = await UserManager.FindByIdAsync(userId);
            // 在此处处理您的正常功能逻辑
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync($"Hi {user.UserName}!");
            return response;
        }

    }
}

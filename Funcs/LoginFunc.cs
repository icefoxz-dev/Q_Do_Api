using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OrderApiFun.Core.Middlewares;
using OrderApiFun.Core.Services;
using OrderDbLib.Entities;
using OrderHelperLib;
using OrderHelperLib.DtoModels.Users;
using Utls;

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

        [Function(nameof(Anonymous_RegisterApi))]
        public async Task<HttpResponseData> Anonymous_RegisterApi(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            HttpRequestData req,
            FunctionContext context)
        {
            var log = context.GetLogger(nameof(Anonymous_RegisterApi));
            var b = await req.GetBagAsync();
            var data = b.Get<RegisterDto>(0);

            var user = new User { UserName = data.Username, Email = data.Email };
            var result = await UserManager.CreateAsync(user, data.Password);

            if (!result.Succeeded)
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync(string.Join("\n",
                    result.Errors.Select(r => $"{r.Code}:{r.Description}")));
                return errorResponse;
            }

            var token = JwtService.GenerateAccessToken(user);
            var refreshToken = JwtService.GenerateRefreshToken(user);
            var successResponse = req.CreateResponse(HttpStatusCode.OK);
            var loginResult = new LoginResult
            {
                access_token = token,
                refresh_token = refreshToken,
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.UserName,
                    Email = user.Email,
                    Phone = user.PhoneNumber,
                    Name = user.Name
                }
            };
            var bag = DataBag.SerializeWithName(nameof(LoginResult), loginResult);
            await successResponse.WriteStringAsync(bag);
            return successResponse;
        }

        [Function(nameof(Anonymous_LoginApi))]
        public async Task<HttpResponseData> Anonymous_LoginApi(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
            FunctionContext context)
        {
            var log = context.GetLogger(nameof(Anonymous_LoginApi));

            var b = await req.GetBagAsync();
            if (b == null)
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync("Invalid request.");
                return errorResponse;
            }
            var loginModel = b.Get<LoginDto>(0);

            var user = await UserManager.FindByNameAsync(loginModel.Username);
            if (user == null)
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync("Invalid username or password.");
                return errorResponse;
            }

            var isValidPassword = await UserManager.CheckPasswordAsync(user, loginModel.Password);
            if (!isValidPassword)
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync("Invalid username or password.");
                return errorResponse;
            }

            var token = JwtService.GenerateAccessToken(user);
            var refreshToken = JwtService.GenerateRefreshToken(user);
            var successResponse = req.CreateResponse(HttpStatusCode.OK);
            var result = new LoginResult
            {
                access_token = token,
                refresh_token = refreshToken,
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.UserName,
                    Email = user.Email,
                    Phone = user.PhoneNumber,
                    Name = user.Name
                }
            };
            var bag = DataBag.SerializeWithName(nameof(LoginResult), result);
            await successResponse.WriteStringAsync(bag);
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

            var bag = await req.GetBagAsync();
            var username = bag?.Get<string>(0) ?? null;
            if (bag == null || string.IsNullOrWhiteSpace(username))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                if (!hasRefreshToken) await badRequestResponse.WriteStringAsync("Username not found!");
                return badRequestResponse;
            }

            var isValid = await JwtService.ValidateRefreshTokenAsync(refreshToken, username);
            if(!isValid)
            {
                log.LogError("Invalid refresh token");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Invalid refresh token");
                return badRequestResponse;
            }

            var user = await UserManager.FindByNameAsync(username);
            if (user == null)
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("User not found");
                return badRequestResponse;
            }

            var newAccessToken = JwtService.GenerateAccessToken(user);

            var okResponse = req.CreateResponse(HttpStatusCode.OK);
            okResponse.Headers.Add("Content-Type", "application/json");
            var loginResult = new LoginResult
            {
                access_token = newAccessToken,
                refresh_token = refreshToken,
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.UserName,
                    Email = user.Email,
                    Phone = user.PhoneNumber,
                    Name = user.Name
                }
            };
            await okResponse.WriteStringAsync(DataBag.SerializeWithName(nameof(loginResult), loginResult));
            return okResponse;

        }

        private string GetValueFromHeader(HttpRequestData req,string header)
        {
            req.Headers.TryGetValues(header, out var tokenValues);
            var tokenArray = tokenValues?.ToArray() ?? null;
            var refreshToken = (tokenArray?.FirstOrDefault() ?? null) ?? string.Empty;
            return refreshToken;
        }

        [Function(nameof(User_TestUserApi))]
        public async Task<HttpResponseData> User_TestUserApi(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req, FunctionContext context)
        {
            // 检查是否存在 HttpResponseData 对象
            if (context.Items.TryGetValue("HttpResponseData", out var item))
            {
                // 如果存在，表示验证失败，直接返回这个 HttpResponseData 对象
                return (HttpResponseData)item;
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

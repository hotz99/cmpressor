using AutoMapper;
using repositories;
using models.users;
using exceptions;
using util;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace services;

// do we really need interfaces ?
public interface IUserService
{
    Task<string> GenerateToken(entities.User user);
    Task<IEnumerable<entities.User>> GetAll();
    Task<entities.User> GetById(int id);
    Task<entities.User> GetByEmail(string email);
    Task<Result<string>> Create(CreateRequest model);
    Task Update(int id, UpdateRequest model);
    Task Delete(int id);
}

public class UserService : IUserService
{
    private IUserRepository _userRepository;
    private readonly IMapper _mapper;

    private readonly JwtSettings _jwtSettings;

    public UserService(
        IUserRepository userRepository,
        IMapper mapper,
        JwtSettings jwtSettings)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _jwtSettings = jwtSettings;
    }

    public async Task<string> GenerateToken(entities.User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            // more claims means less db lookups
            Subject = new ClaimsIdentity(
            [
            new Claim("UserId", user.UserId.ToString()),
            new Claim("SubscriptionType", user.SubscriptionPlan is null ? "Free" : "Paid")
        ]),
            Expires = DateTime.UtcNow.AddDays(_jwtSettings.TokenLifetimeDays),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }


    public async Task<IEnumerable<entities.User>> GetAll()
    {
        return await _userRepository.GetAll();
    }

    public async Task<entities.User> GetById(int id)
    {
        return await _userRepository.GetById(id);
    }

    public async Task<entities.User> GetByEmail(string email)
    {
        return await _userRepository.GetByEmail(email);
    }

    public async Task<Result<string>> Create(CreateRequest model)
    {
        if (await _userRepository.GetByEmail(model.Email!) != null)
            return Result<string>.Failure("User with this email already exists.");

        var user = _mapper.Map<entities.User>(model);

        // hash password
        var updatedUser = user with { PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password) };

        // save user
        await _userRepository.Create(updatedUser);

        return Result<string>.Success(await GenerateToken(updatedUser));
    }

    public async Task Update(int id, UpdateRequest model)
    {
        var user = await _userRepository.GetById(id);

        if (user == null)
            throw new KeyNotFoundException("User not found");

        // validate
        var emailChanged = !string.IsNullOrEmpty(model.Email) && user.Email != model.Email;
        if (emailChanged && await _userRepository.GetByEmail(model.Email!) != null)
            throw new AppException("User with the email '" + model.Email + "' already exists");

        // copy model props to user
        _mapper.Map(model, user);

        // hash password if it was entered
        if (!string.IsNullOrEmpty(model.Password))
        {
            var updatedUser = user with { PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password) };
            await _userRepository.Update(updatedUser);
            return;
        }

        // save user
        await _userRepository.Update(user);
    }

    public async Task Delete(int id)
    {
        await _userRepository.Delete(id);
    }
}

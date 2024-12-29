using AutoMapper;
using repositories;
using entities;
using models.users;
using exceptions;
using util;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace services;
public interface IUserService
{
    Task<string> GenerateToken(entities.User user);
    Task<IEnumerable<User>> GetAll();
    Task<User> GetById(int id);
    Task<User> GetByEmail(string email);
    Task<string> Create(CreateRequest model);
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


    public async Task<IEnumerable<User>> GetAll()
    {
        return await _userRepository.GetAll();
    }

    public async Task<User> GetById(int id)
    {
        var user = await _userRepository.GetById(id);

        if (user == null)
            throw new KeyNotFoundException("User not found");

        return user;
    }

    public async Task<User> GetByEmail(string email)
    {
        var user = await _userRepository.GetByEmail(email);

        if (user == null)
            throw new KeyNotFoundException("User not found");

        return user;
    }

    public async Task<string> Create(CreateRequest model)
    {
        // validate
        if (await _userRepository.GetByEmail(model.Email!) != null)
            throw new AppException("User with the email '" + model.Email + "' already exists");

        // map model to new user object
        var user = _mapper.Map<User>(model);

        // hash password
        var updatedUser = user with { PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password) };

        // save user
        await _userRepository.Create(updatedUser);

        return await GenerateToken(updatedUser);
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
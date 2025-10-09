using AutoMapper;
using BCrypt.Net;
using InsuranceClaimsAPI.Data;
using InsuranceClaimsAPI.Models.Domain;
using InsuranceClaimsAPI.Models.DTOs.Auth;
using Microsoft.EntityFrameworkCore;

namespace InsuranceClaimsAPI.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginRequestDto loginRequest);
        Task<AuthResponseDto> RegisterAsync(RegisterRequestDto registerRequest);
        Task<AddUserResponseDto> AddUserAsync(AddUserRequestDto addUserRequest);
        Task<bool> EmailExistsAsync(string email);
        Task<UserDto?> GetUserByFirebaseUidAsync(string firebaseUid);
        Task<List<UserDto>> GetAllUsersAsync();
        Task<List<UserDto>> GetUsersByRoleAsync(UserRole role);
        Task<List<UserDto>> GetInsurersAsync();
        Task<List<UserDto>> GetProvidersAsync();
    }

    public class AuthService : IAuthService
    {
        private readonly InsuranceClaimsContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            InsuranceClaimsContext context,
            IMapper mapper,
            ILogger<AuthService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto loginRequest)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == loginRequest.Email && u.Status == UserStatus.Active);

                if (user == null || !BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Login attempt failed for email: {Email}", loginRequest.Email);
                    throw new UnauthorizedAccessException("Invalid email or password");
                }

                // Update last login
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var userDto = _mapper.Map<UserDto>(user);

                return new AuthResponseDto
                {
                    User = userDto,
                    Message = "Login successful"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during login for email: {Email}", loginRequest.Email);
                throw;
            }
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto registerRequest)
        {
            try
            {
                if (await EmailExistsAsync(registerRequest.Email))
                {
                    throw new InvalidOperationException("Email already exists");
                }

                var user = new User
                {
                    FirstName = registerRequest.FirstName,
                    LastName = registerRequest.LastName,
                    Email = registerRequest.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerRequest.Password),
                    FirebaseUid = registerRequest.FirebaseUid,
                    PhoneNumber = registerRequest.PhoneNumber,
                    CompanyName = registerRequest.CompanyName,
                    Address = registerRequest.Address,
                    City = registerRequest.City,
                    PostalCode = registerRequest.PostalCode,
                    Country = registerRequest.Country,
                    Role = (UserRole)registerRequest.Role,
                    Status = UserStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var userDto = _mapper.Map<UserDto>(user);

                return new AuthResponseDto
                {
                    User = userDto,
                    Message = "Registration successful"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during registration for email: {Email}", registerRequest.Email);
                throw;
            }
        }

        public async Task<AddUserResponseDto> AddUserAsync(AddUserRequestDto addUserRequest)
        {
            try
            {
                if (await EmailExistsAsync(addUserRequest.Email))
                {
                    throw new InvalidOperationException("Email already exists");
                }

                var user = new User
                {
                    FirstName = addUserRequest.FirstName,
                    LastName = addUserRequest.LastName,
                    Email = addUserRequest.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(addUserRequest.Password),
                    FirebaseUid = addUserRequest.FirebaseUid,
                    PhoneNumber = addUserRequest.PhoneNumber,
                    CompanyName = addUserRequest.CompanyName,
                    Address = addUserRequest.Address,
                    City = addUserRequest.City,
                    PostalCode = addUserRequest.PostalCode,
                    Country = addUserRequest.Country,
                    Role = (UserRole)addUserRequest.Role,
                    Status = addUserRequest.Status,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return new AddUserResponseDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    CompanyName = user.CompanyName,
                    Address = user.Address,
                    City = user.City,
                    PostalCode = user.PostalCode,
                    Country = user.Country,
                    Role = user.Role,
                    Status = user.Status,
                    CreatedAt = user.CreatedAt,
                    Message = "User added successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding user with email: {Email}", addUserRequest.Email);
                throw;
            }
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<UserDto?> GetUserByFirebaseUidAsync(string firebaseUid)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUid && u.Status == UserStatus.Active);

                if (user == null)
                {
                    return null;
                }

                return _mapper.Map<UserDto>(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting user by Firebase UID: {FirebaseUid}", firebaseUid);
                throw;
            }
        }

        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            try
            {
                var users = await _context.Users
                    .Where(u => u.Status == UserStatus.Active)
                    .ToListAsync();

                return _mapper.Map<List<UserDto>>(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all users");
                throw;
            }
        }

        public async Task<List<UserDto>> GetUsersByRoleAsync(UserRole role)
        {
            try
            {
                var users = await _context.Users
                    .Where(u => u.Role == role && u.Status == UserStatus.Active)
                    .ToListAsync();

                return _mapper.Map<List<UserDto>>(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting users by role: {Role}", role);
                throw;
            }
        }

        public async Task<List<UserDto>> GetInsurersAsync()
        {
            return await GetUsersByRoleAsync(UserRole.Insurer);
        }

        public async Task<List<UserDto>> GetProvidersAsync()
        {
            return await GetUsersByRoleAsync(UserRole.Provider);
        }

    }
}

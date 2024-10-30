using AutoMapper;
using BLL.Shared;
using DAL.Abstraction;
using DAL.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class UsersService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _memoryCache ;
        private readonly IDistributedCache _distributedCache;

        public UsersService(IUnitOfWork unitOfWork, IMapper mapper,IMemoryCache memoryCache, IDistributedCache distributedCache)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _memoryCache = memoryCache;
            _distributedCache = distributedCache;
        }

        public async Task<IEnumerable<UserDTO>> GetAllUsersAsync(string searchTerm = null, string sortBy = "UserName", bool ascending = true, int page = 1, int pageSize = 10)
        {
            var users = await _unitOfWork.Users.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                users = users.Where(u => u.UserName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                          u.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            }

            users = sortBy switch
            {
                "Email" => ascending ? users.OrderBy(u => u.Email) : users.OrderByDescending(u => u.Email),
                _ => ascending ? users.OrderBy(u => u.UserName) : users.OrderByDescending(u => u.UserName),
            };

            return users.Skip((page - 1) * pageSize).Take(pageSize).Select(user => _mapper.Map<UserDTO>(user)).ToList();
        }

        public async Task<UserDTO> GetUserByIdAsync(Guid id)
        {
            string cacheKey = $"User_{id}";
            var cachedUser = await _distributedCache.GetStringAsync(cacheKey);
            if (cachedUser != null)
            {
                return JsonSerializer.Deserialize<UserDTO>(cachedUser);
            }
            if (_memoryCache.TryGetValue(cacheKey, out UserDTO user))
            {
                return user;
            }

            var userEntity = await _unitOfWork.Users.GetByIdAsync(id);
            if (userEntity == null)
            {
                return null;
            }
            user = _mapper.Map<UserDTO>(userEntity);
            _memoryCache.Set(cacheKey, user, new MemoryCacheEntryOptions//додавання в in-Memory Cache
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                Size = 1
            });
            var serializedUser = JsonSerializer.Serialize(user);
            await _distributedCache.SetStringAsync(cacheKey, serializedUser, new DistributedCacheEntryOptions//додавання в Redis Cache
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });

            return user;
        }

        public async Task AddUserAsync(UserDTO userDto)
        {
            var user = _mapper.Map<User>(userDto);
            user.PasswordHash = HashPassword(userDto.PasswordHash);
            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.CompleteAsync();
        }
        private string HashPassword(string password)
        {
            
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
        public async Task UpdateUserAsync(UserDTO userDto)
        {
            var user = _mapper.Map<User>(userDto);
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.CompleteAsync();
        }

        public async Task DeleteUserAsync(Guid id)
        {
            await _unitOfWork.Users.DeleteAsync(id);
            await _unitOfWork.CompleteAsync();
        }
    }
}

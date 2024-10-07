using AutoMapper;
using BLL.Shared;
using DAL.Abstraction;
using DAL.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class UsersService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UsersService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
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
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            return _mapper.Map<UserDTO>(user);
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

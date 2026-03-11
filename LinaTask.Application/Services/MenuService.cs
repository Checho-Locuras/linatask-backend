using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.DTOs;
using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinaTask.Application.Services
{
    public class MenuService : IMenuService
    {
        private readonly IMenuRepository _repository;

        public MenuService(IMenuRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<MenuDto>> GetAllAsync()
        {
            var menus = await _repository.GetAllAsync();
            return menus.Select(MapToDto);
        }

        public async Task<IEnumerable<MenuDto>> GetVisibleMenusAsync()
        {
            var menus = await _repository.GetVisibleMenusAsync();
            return menus.Select(MapToDto);
        }

        public async Task<MenuWithChildrenDto?> GetByIdAsync(Guid id)
        {
            var menu = await _repository.GetByIdAsync(id);
            if (menu == null)
                return null;

            var dto = MapToWithChildrenDto(menu);
            dto.PermissionIds = (await _repository.GetPermissionIdsAsync(id)).ToList();

            return dto;
        }

        public async Task<IEnumerable<MenuWithChildrenDto>> GetMenuHierarchyAsync()
        {
            var menus = await _repository.GetMenuHierarchyAsync();
            return menus.Select(m => BuildHierarchyDto(m)).ToList();
        }

        public async Task<MenuDto> CreateAsync(CreateMenuDto dto)
        {
            var menu = new Menu
            {
                Name = dto.Name,
                Icon = dto.Icon,
                Route = dto.Route,
                ParentId = dto.ParentId,
                Order = dto.Order,
                IsVisible = dto.IsVisible
            };

            var created = await _repository.CreateAsync(menu);

            if (dto.PermissionIds.Any())
            {
                await _repository.AssignPermissionsAsync(created.Id, dto.PermissionIds);
            }

            return MapToDto(created);
        }

        public async Task<MenuDto?> UpdateAsync(Guid id, UpdateMenuDto dto)
        {
            var menu = await _repository.GetByIdAsync(id);
            if (menu == null)
                return null;

            if (dto.Name != null)
                menu.Name = dto.Name;
            if (dto.Icon != null)
                menu.Icon = dto.Icon;
            if (dto.Route != null)
                menu.Route = dto.Route;
            if (dto.ParentId != null)
                menu.ParentId = dto.ParentId;
            if (dto.Order != null)
                menu.Order = dto.Order.Value;
            if (dto.IsVisible != null)
                menu.IsVisible = dto.IsVisible.Value;

            var updated = await _repository.UpdateAsync(menu);

            if (dto.PermissionIds != null)
            {
                await _repository.AssignPermissionsAsync(id, dto.PermissionIds);
            }

            return MapToDto(updated);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var hasChildren = await _repository.HasChildrenAsync(id);
            if (hasChildren)
                throw new InvalidOperationException("No se puede eliminar un menú que tiene hijos");

            return await _repository.DeleteAsync(id);
        }

        public async Task AssignPermissionsAsync(Guid menuId, List<Guid> permissionIds)
        {
            await _repository.AssignPermissionsAsync(menuId, permissionIds);
        }

        // Métodos privados de mapeo
        private MenuDto MapToDto(Menu menu)
        {
            return new MenuDto
            {
                Id = menu.Id,
                Name = menu.Name,
                Icon = menu.Icon,
                Route = menu.Route,
                ParentId = menu.ParentId,
                Order = menu.Order,
                IsVisible = menu.IsVisible
            };
        }

        private MenuWithChildrenDto MapToWithChildrenDto(Menu menu)
        {
            return new MenuWithChildrenDto
            {
                Id = menu.Id,
                Name = menu.Name,
                Icon = menu.Icon,
                Route = menu.Route,
                ParentId = menu.ParentId,
                Order = menu.Order,
                IsVisible = menu.IsVisible,
                Children = menu.Children?.Select(c => MapToWithChildrenDto(c)).ToList() ?? new()
            };
        }

        private MenuWithChildrenDto BuildHierarchyDto(Menu menu)
        {
            var dto = MapToWithChildrenDto(menu);
            dto.Children = menu.Children?
                .OrderBy(c => c.Order)
                .ThenBy(c => c.Name)
                .Select(c => BuildHierarchyDto(c))
                .ToList() ?? new();

            return dto;
        }

        public async Task<IEnumerable<MenuWithChildrenDto>> GetMenusByRoleAsync(Guid roleId)
        {
            return await _repository.GetMenusByRoleIdAsync(roleId);
        }

        public async Task<IEnumerable<MenuWithChildrenDto>> GetMenusByRoleNameAsync(string roleName)
        {
            return await _repository.GetMenusByRoleNameAsync(roleName);
        }
    }
}
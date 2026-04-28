using System.Reflection;
using Api.Data.Entities;
using Api.Interfaces;
using EfCoreRepository.Interfaces;
using Shared.Contracts;

namespace Api.Services;

public sealed class GlobalConfigService(IEfRepository repository, IServiceProvider serviceProvider, ILogger<GlobalConfigService> logger) : IGlobalConfigService
{
    private static GlobalConfigModel? _current;

    private IBasicCrud<GlobalConfig> Dal => repository.For<GlobalConfig>();

    private static readonly List<(PropertyInfo Property, GlobalConfigColAttribute Attr)> ConfigProperties =
        typeof(GlobalConfigModel).GetProperties()
            .Select(p => (Property: p, Attr: p.GetCustomAttribute<GlobalConfigColAttribute>()))
            .Where(x => x.Attr is not null)
            .Select(x => (x.Property, x.Attr!))
            .ToList();

    public async Task<GlobalConfigModel> GetAsync()
    {
        var rows = (await Dal.GetAll()).ToDictionary(r => r.Key, r => r.Value);
        var model = new GlobalConfigModel();

        foreach (var (property, attr) in ConfigProperties)
        {
            if (rows.TryGetValue(attr.Name, out var value))
            {
                var converted = Convert.ChangeType(value, property.PropertyType);
                property.SetValue(model, converted);
            }
        }

        _current = model;
        return model;
    }

    public async Task SaveAsync(GlobalConfigModel config)
    {
        var previous = _current ?? await GetAsync();
        var rows = (await Dal.GetAll()).ToList();

        foreach (var (property, attr) in ConfigProperties)
        {
            var newValue = property.GetValue(config)?.ToString() ?? string.Empty;
            var oldValue = property.GetValue(previous)?.ToString() ?? string.Empty;

            if (newValue != oldValue)
            {
                newValue = InvokeHandler(attr, newValue);
            }

            var existing = rows.FirstOrDefault(r => r.Key == attr.Name);

            if (existing is not null)
            {
                await Dal.Update(existing.Id, e => e.Value = newValue);
            }
            else
            {
                await Dal.Save(new GlobalConfig
                {
                    Key = attr.Name,
                    Value = newValue
                });
            }
        }

        _current = config;
    }

    public async Task InitAsync()
    {
        var config = await GetAsync();

        foreach (var (property, attr) in ConfigProperties)
        {
            var value = property.GetValue(config)?.ToString() ?? string.Empty;
            InvokeHandler(attr, value);
        }
    }

    private string InvokeHandler(GlobalConfigColAttribute attr, string value)
    {
        if (attr.OnChangeHandler is null)
        {
            return value;
        }

        if (serviceProvider.GetService(attr.OnChangeHandler) is IGlobalConfigChangeHandler handler)
        {
            return handler.OnChange(value);
        }

        logger.LogError("No IGlobalConfigChangeHandler registered for {HandlerType}. Register it in DI", attr.OnChangeHandler.FullName);
        return value;
    }
}

using LiteDB;
using Milk_manager.Models;

namespace Milk_manager.Services;

public class DatabaseService
{
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<Village> _villages;
    private readonly ILiteCollection<Client> _clients;
    private readonly ILiteCollection<Factory> _factories;
    private readonly ILiteCollection<MilkPurchase> _purchases;
    private readonly ILiteCollection<ClientPayment> _payments;
    private readonly ILiteCollection<FactoryDelivery> _deliveries;
    private readonly ILiteCollection<MilkWriteOff> _writeOffs;

    // singleton instance for simple access from pages
    public static DatabaseService Instance { get; } = new();

    public DatabaseService()
    {
        var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "milk_manager.db");
        _db = new LiteDatabase(dbPath);

        _villages = _db.GetCollection<Village>("villages");
        _clients = _db.GetCollection<Client>("clients");
        _factories = _db.GetCollection<Factory>("factories");
        _purchases = _db.GetCollection<MilkPurchase>("purchases");
        _payments = _db.GetCollection<ClientPayment>("payments");
        _deliveries = _db.GetCollection<FactoryDelivery>("deliveries");
        _writeOffs = _db.GetCollection<MilkWriteOff>("writeoffs");

        _villages.EnsureIndex(village => village.Name);
        _clients.EnsureIndex(client => client.VillageId);
        _factories.EnsureIndex(factory => factory.Name);
        _payments.EnsureIndex(payment => payment.ClientId);

        SeedInitialData();
    }

    public Task<List<Village>> GetVillagesAsync()
    {
        return Task.Run(() => _villages.FindAll().OrderBy(village => village.Name).ToList());
    }

    public Task<Village> GetOrCreateVillageAsync(string villageName)
    {
        return Task.Run(() =>
        {
            var normalizedName = string.IsNullOrWhiteSpace(villageName) ? "Без поселка" : villageName.Trim();
            var village = _villages.FindAll()
                .FirstOrDefault(item => string.Equals(item.Name, normalizedName, StringComparison.CurrentCultureIgnoreCase));
            if (village is not null)
            {
                return village;
            }

            village = new Village { Name = normalizedName };
            _villages.Insert(village);
            DeveloperActionLogService.Log(
                "Village.Create",
                $"Создан поселок '{village.Name}'.",
                after: village,
                rollbackHint: $"Удалить поселок Id={village.Id}, если к нему не привязаны клиенты.");
            return village;
        });
    }

    public Task<List<Factory>> GetFactoriesAsync()
    {
        return Task.Run(() => _factories.FindAll().OrderBy(factory => factory.Name).ToList());
    }

    public Task AddFactoryAsync(Factory factory)
    {
        return Task.Run(() =>
        {
            _factories.Insert(factory);
            DeveloperActionLogService.Log(
                "Factory.Create",
                $"Создан завод '{factory.Name}'.",
                after: factory,
                rollbackHint: $"Удалить завод Id={factory.Id}.");
        });
    }

    public Task UpdateFactoryAsync(Factory factory)
    {
        return Task.Run(() =>
        {
            var before = _factories.FindById(factory.Id);
            _factories.Update(factory);
            DeveloperActionLogService.Log(
                "Factory.Update",
                $"Изменен завод Id={factory.Id}: '{before?.Name}' -> '{factory.Name}'.",
                before: before,
                after: factory,
                rollbackHint: "Вернуть значения завода из before.");
        });
    }

    public Task DeleteFactoryAsync(int factoryId)
    {
        return Task.Run(() =>
        {
            var before = _factories.FindById(factoryId);
            _factories.Delete(factoryId);
            DeveloperActionLogService.Log(
                "Factory.Delete",
                $"Удален завод Id={factoryId} ('{before?.Name}').",
                before: before,
                rollbackHint: "Создать завод заново по данным before.");
        });
    }

    public Task<List<Client>> GetClientsByVillageAsync(int villageId)
    {
        return Task.Run(() => _clients.Find(client => client.VillageId == villageId).OrderBy(client => client.FullName).ToList());
    }

    public Task<List<Client>> GetAllClientsAsync()
    {
        return Task.Run(() => _clients.FindAll().OrderBy(client => client.FullName).ToList());
    }

    public Task AddClientAsync(Client client)
    {
        return Task.Run(() =>
        {
            _clients.Insert(client);
            DeveloperActionLogService.Log(
                "Client.Create",
                $"Создан клиент '{client.FullName}' с ценой {client.DefaultPrice:F2}.",
                after: client,
                rollbackHint: $"Удалить клиента Id={client.Id}, если запись была ошибочной.");
        });
    }

    public Task UpdateClientAsync(Client client)
    {
        return Task.Run(() =>
        {
            var before = _clients.FindById(client.Id);
            _clients.Update(client);
            DeveloperActionLogService.Log(
                "Client.Update",
                $"Изменен клиент Id={client.Id}: '{before?.FullName}' -> '{client.FullName}', цена {before?.DefaultPrice:F2} -> {client.DefaultPrice:F2}.",
                before: before,
                after: client,
                rollbackHint: "Вернуть значения клиента из before.");
        });
    }

    public Task DeleteClientAsync(int clientId)
    {
        return Task.Run(() =>
        {
            var before = _clients.FindById(clientId);
            var purchaseCount = _purchases.Count(purchase => purchase.ClientId == clientId);
            var paymentCount = _payments.Count(payment => payment.ClientId == clientId);
            _clients.Delete(clientId);
            _purchases.DeleteMany(purchase => purchase.ClientId == clientId);
            _payments.DeleteMany(payment => payment.ClientId == clientId);
            DeveloperActionLogService.Log(
                "Client.Delete",
                $"Удален клиент Id={clientId} ('{before?.FullName}'), также удалено приемов: {purchaseCount}, выплат: {paymentCount}.",
                before: new { Client = before, PurchasesDeleted = purchaseCount, PaymentsDeleted = paymentCount },
                rollbackHint: "Создать клиента заново по before; связанные приемы/выплаты восстановить из резервной копии LiteDB.");
        });
    }

    public Task AddPurchaseAsync(MilkPurchase purchase)
    {
        return Task.Run(() =>
        {
            if (purchase.Date == default)
            {
                purchase.Date = DateTime.Now;
            }

            _purchases.Insert(purchase);
            var client = _clients.FindById(purchase.ClientId);
            DeveloperActionLogService.Log(
                "Purchase.Create",
                $"Прием молока: клиент '{client?.FullName ?? purchase.ClientId.ToString()}', {purchase.Liters:F1} л × {purchase.PricePerLiter:F2} = {purchase.TotalSum:F2}.",
                after: new { Purchase = purchase, Client = client },
                rollbackHint: $"Удалить прием Id={purchase.Id}, если запись ошибочная.");
        });
    }

    public Task AddPaymentAsync(ClientPayment payment)
    {
        return Task.Run(() =>
        {
            if (payment.Date == default)
            {
                payment.Date = DateTime.Now;
            }

            _payments.Insert(payment);
            var client = _clients.FindById(payment.ClientId);
            DeveloperActionLogService.Log(
                "Payment.Create",
                $"Выплата клиенту '{client?.FullName ?? payment.ClientId.ToString()}': {payment.Amount:F2}. Комментарий: {payment.Comment ?? "без комментария"}.",
                after: new { Payment = payment, Client = client },
                rollbackHint: $"Удалить выплату Id={payment.Id}, если запись ошибочная.");
        });
    }

    public Task AddDeliveryAsync(FactoryDelivery delivery)
    {
        return Task.Run(() =>
        {
            if (delivery.Date == default)
            {
                delivery.Date = DateTime.Now;
            }

            _deliveries.Insert(delivery);
            DeveloperActionLogService.Log(
                "Delivery.Create",
                $"Сдача на завод '{delivery.FactoryName}': {delivery.Liters:F1} л × {delivery.PricePerLiter:F2} = {delivery.TotalSum:F2}.",
                after: delivery,
                rollbackHint: $"Удалить сдачу Id={delivery.Id}, если запись ошибочная.");
        });
    }

    public Task AddWriteOffAsync(MilkWriteOff writeOff)
    {
        return Task.Run(() =>
        {
            if (writeOff.Date == default)
            {
                writeOff.Date = DateTime.Now;
            }

            _writeOffs.Insert(writeOff);
            DeveloperActionLogService.Log(
                "WriteOff.Create",
                $"Списание молока: {writeOff.Liters:F1} л. Причина: {writeOff.Reason ?? "не указана"}.",
                after: writeOff,
                rollbackHint: $"Удалить списание Id={writeOff.Id}, если запись ошибочная.");
        });
    }

    public Task<List<PurchaseEntryRow>> GetPurchaseEntriesForDayAsync(DateTime date)
    {
        return Task.Run(() =>
        {
            var from = date.Date;
            var to = from.AddDays(1).AddTicks(-1);
            var clientsById = _clients.FindAll().ToDictionary(client => client.Id);
            return _purchases.Find(purchase => purchase.Date >= from && purchase.Date <= to)
                .OrderByDescending(purchase => purchase.Date)
                .Select(purchase =>
                {
                    clientsById.TryGetValue(purchase.ClientId, out var client);
                    return new PurchaseEntryRow(
                        client?.FullName ?? $"Клиент #{purchase.ClientId}",
                        client?.Phone ?? string.Empty,
                        purchase.Liters,
                        purchase.PricePerLiter);
                })
                .ToList();
        });
    }

    public Task<List<PaymentEntryRow>> GetPaymentEntriesForDayAsync(DateTime date)
    {
        return Task.Run(() =>
        {
            var from = date.Date;
            var to = from.AddDays(1).AddTicks(-1);
            var clientsById = _clients.FindAll().ToDictionary(client => client.Id);
            var purchasesByClient = _purchases.Find(purchase => purchase.Date >= from && purchase.Date <= to)
                .GroupBy(purchase => purchase.ClientId)
                .ToDictionary(
                    group => group.Key,
                    group => new
                    {
                        Liters = group.Sum(purchase => purchase.Liters),
                        Total = group.Sum(purchase => purchase.TotalSum)
                    });

            return _payments.Find(payment => payment.Date >= from && payment.Date <= to)
                .OrderByDescending(payment => payment.Date)
                .Select(payment =>
                {
                    clientsById.TryGetValue(payment.ClientId, out var client);
                    purchasesByClient.TryGetValue(payment.ClientId, out var purchaseTotal);
                    var liters = purchaseTotal?.Liters ?? 0;
                    var milkTotal = purchaseTotal?.Total ?? 0m;
                    var price = liters > 0 ? milkTotal / (decimal)liters : (client?.DefaultPrice ?? 0m);
                    return new PaymentEntryRow(
                        client?.FullName ?? $"Клиент #{payment.ClientId}",
                        client?.Phone ?? string.Empty,
                        payment.Amount,
                        payment.Comment ?? string.Empty,
                        liters,
                        price,
                        milkTotal);
                })
                .ToList();
        });
    }

    public Task<List<DeliveryEntryRow>> GetDeliveryEntriesForDayAsync(DateTime date)
    {
        return Task.Run(() =>
        {
            var from = date.Date;
            var to = from.AddDays(1).AddTicks(-1);
            return _deliveries.Find(delivery => delivery.Date >= from && delivery.Date <= to)
                .OrderByDescending(delivery => delivery.Date)
                .Select(delivery => new DeliveryEntryRow(delivery.FactoryName, delivery.Liters, delivery.PricePerLiter))
                .ToList();
        });
    }

    public Task<List<WriteOffEntryRow>> GetWriteOffEntriesAsync()
    {
        return Task.Run(() => _writeOffs.FindAll()
            .OrderByDescending(writeOff => writeOff.Date)
            .Select(writeOff => new WriteOffEntryRow(writeOff.Date, writeOff.Liters, writeOff.Reason ?? string.Empty))
            .ToList());
    }

    public Task<ReportData> GetReportDataAsync(DateTime fromDate, DateTime toDate)
    {
        return Task.Run(() =>
        {
            var from = fromDate.Date;
            var to = toDate.Date.AddDays(1).AddTicks(-1);
            var villagesById = _villages.FindAll().ToDictionary(village => village.Id, village => village.Name);
            var purchases = _purchases.Find(purchase => purchase.Date >= from && purchase.Date <= to).ToList();
            var payments = _payments.Find(payment => payment.Date >= from && payment.Date <= to).ToList();
            var deliveries = _deliveries.Find(delivery => delivery.Date >= from && delivery.Date <= to).ToList();

            var purchasesByClient = purchases
                .GroupBy(purchase => purchase.ClientId)
                .ToDictionary(
                    group => group.Key,
                    group => new
                    {
                        Liters = group.Sum(purchase => purchase.Liters),
                        Total = group.Sum(purchase => purchase.TotalSum)
                    });
            var paymentsByClient = payments
                .GroupBy(payment => payment.ClientId)
                .ToDictionary(group => group.Key, group => group.Sum(payment => payment.Amount));

            var clients = _clients.FindAll()
                .OrderBy(client => villagesById.GetValueOrDefault(client.VillageId, "Без поселка"))
                .ThenBy(client => client.FullName)
                .Select(client =>
                {
                    purchasesByClient.TryGetValue(client.Id, out var purchaseTotal);
                    var paid = paymentsByClient.GetValueOrDefault(client.Id);
                    var purchased = purchaseTotal?.Total ?? 0m;

                    return new ClientBalanceReportRow
                    {
                        ClientName = client.FullName,
                        VillageName = villagesById.GetValueOrDefault(client.VillageId, "Без поселка"),
                        LitersTaken = purchaseTotal?.Liters ?? 0,
                        TotalAmount = purchased,
                        PaidAmount = paid,
                        DebtAmount = purchased - paid
                    };
                })
                .ToList();

            var factories = deliveries
                .GroupBy(delivery => string.IsNullOrWhiteSpace(delivery.FactoryName) ? "Без названия" : delivery.FactoryName.Trim())
                .OrderBy(group => group.Key)
                .Select(group => new FactoryBalanceReportRow
                {
                    FactoryName = group.Key,
                    LitersDelivered = group.Sum(delivery => delivery.Liters),
                    TotalAmount = group.Sum(delivery => delivery.TotalSum),
                    DeliveriesCount = group.Count()
                })
                .ToList();

            var purchaseDays = purchases
                .GroupBy(purchase => purchase.Date.Date)
                .ToDictionary(
                    group => group.Key,
                    group => new
                    {
                        Liters = group.Sum(purchase => purchase.Liters),
                        Total = group.Sum(purchase => purchase.TotalSum)
                    });
            var deliveryDays = deliveries
                .GroupBy(delivery => delivery.Date.Date)
                .ToDictionary(
                    group => group.Key,
                    group => new
                    {
                        Liters = group.Sum(delivery => delivery.Liters),
                        Total = group.Sum(delivery => delivery.TotalSum)
                    });
            var allDays = purchaseDays.Keys
                .Union(deliveryDays.Keys)
                .OrderBy(day => day)
                .Select(day => new DailyReportRow
                {
                    Date = day,
                    ClientLiters = purchaseDays.GetValueOrDefault(day)?.Liters ?? 0,
                    ClientAmount = purchaseDays.GetValueOrDefault(day)?.Total ?? 0m,
                    FactoryLiters = deliveryDays.GetValueOrDefault(day)?.Liters ?? 0,
                    FactoryAmount = deliveryDays.GetValueOrDefault(day)?.Total ?? 0m
                })
                .ToList();

            return new ReportData
            {
                FromDate = from,
                ToDate = toDate.Date,
                Clients = clients,
                Factories = factories,
                Days = allDays,
                Totals = new ReportTotals
                {
                    ClientLiters = clients.Sum(client => client.LitersTaken),
                    ClientAmount = clients.Sum(client => client.TotalAmount),
                    ClientPaid = clients.Sum(client => client.PaidAmount),
                    ClientDebt = clients.Sum(client => client.DebtAmount),
                    FactoryLiters = factories.Sum(factory => factory.LitersDelivered),
                    FactoryAmount = factories.Sum(factory => factory.TotalAmount)
                }
            };
        });
    }

    public Task<List<ClientBalanceReportRow>> GetClientBalanceReportAsync()
    {
        var today = DateTime.Today;
        return GetReportDataAsync(DateTime.MinValue.Date, today).ContinueWith(task => task.Result.Clients);
    }

    public Task<List<FactoryBalanceReportRow>> GetFactoryBalanceReportAsync()
    {
        var today = DateTime.Today;
        return GetReportDataAsync(DateTime.MinValue.Date, today).ContinueWith(task => task.Result.Factories);
    }

    private void SeedInitialData()
    {
        if (_villages.Count() != 0)
        {
            return;
        }

        var lowerVillage = new Village { Name = "Нижний Поселок" };
        var mountainVillage = new Village { Name = "Горный" };
        _villages.Insert(lowerVillage);
        _villages.Insert(mountainVillage);

        _factories.Insert(new Factory { Name = "Главный молокозавод", Phone = "+79990000003" });

        _clients.Insert(new Client { FullName = "Иванов И.И.", VillageId = lowerVillage.Id, Phone = "+79990000001", DefaultPrice = 4.50m });
        _clients.Insert(new Client { FullName = "Петров П.П.", VillageId = mountainVillage.Id, Phone = "+79990000002", DefaultPrice = 4.25m });
    }
}

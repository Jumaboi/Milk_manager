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
            return village;
        });
    }

    public Task<List<Factory>> GetFactoriesAsync()
    {
        return Task.Run(() => _factories.FindAll().OrderBy(factory => factory.Name).ToList());
    }

    public Task AddFactoryAsync(Factory factory)
    {
        return Task.Run(() => _factories.Insert(factory));
    }

    public Task UpdateFactoryAsync(Factory factory)
    {
        return Task.Run(() => _factories.Update(factory));
    }

    public Task DeleteFactoryAsync(int factoryId)
    {
        return Task.Run(() => _factories.Delete(factoryId));
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
        return Task.Run(() => _clients.Insert(client));
    }

    public Task UpdateClientAsync(Client client)
    {
        return Task.Run(() => _clients.Update(client));
    }

    public Task DeleteClientAsync(int clientId)
    {
        return Task.Run(() =>
        {
            _clients.Delete(clientId);
            _purchases.DeleteMany(purchase => purchase.ClientId == clientId);
            _payments.DeleteMany(payment => payment.ClientId == clientId);
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
        });
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

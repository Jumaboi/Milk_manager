using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;
using Milk_manager.Models;

namespace Milk_manager.Services
{

    public class DatabaseService
    {
        private readonly LiteDatabase _db;
        private readonly ILiteCollection<Village> _villages;
        private readonly ILiteCollection<Client> _clients;
        private readonly ILiteCollection<MilkPurchase> _purchases;
        private readonly ILiteCollection<ClientPayment> _payments;
        private readonly ILiteCollection<FactoryDelivery> _deliveries;
        private readonly ILiteCollection<MilkWriteOff> _writeOffs;

        // singleton instance for simple access from pages
        public static DatabaseService Instance { get; } = new DatabaseService();

        public DatabaseService()
        {
            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "milk_manager.db");
            _db = new LiteDatabase(dbPath);

            _villages = _db.GetCollection<Village>("villages");
            _clients = _db.GetCollection<Client>("clients");
            _purchases = _db.GetCollection<MilkPurchase>("purchases");
            _payments = _db.GetCollection<ClientPayment>("payments");
            _deliveries = _db.GetCollection<FactoryDelivery>("deliveries");
            _writeOffs = _db.GetCollection<MilkWriteOff>("writeoffs");

            // Seed данных, если пусто
            Task.Run(() =>
            {
                if (_villages.Count() == 0)
                {
                    var v1 = new Village { Name = "Нижний Поселок" };
                    var v2 = new Village { Name = "Горный" };
                    _villages.Insert(v1);
                    _villages.Insert(v2);

                    _clients.Insert(new Client { FullName = "Иванов И.И.", VillageId = v1.Id, Phone = "", DefaultPrice = 4.50m });
                    _clients.Insert(new Client { FullName = "Петров П.П.", VillageId = v2.Id, Phone = "", DefaultPrice = 4.25m });
                }
            });
        }

        public Task<List<Village>> GetVillagesAsync()
        {
            return Task.Run(() => _villages.FindAll().ToList());
        }

        public Task<List<Client>> GetClientsByVillageAsync(int villageId)
        {
            return Task.Run(() => _clients.Find(c => c.VillageId == villageId).ToList());
        }

        public Task<List<Client>> GetAllClientsAsync()
        {
            return Task.Run(() => _clients.FindAll().ToList());
        }

        public Task AddClientAsync(Client client)
        {
            return Task.Run(() => _clients.Insert(client));
        }

        public Task AddPurchaseAsync(MilkPurchase purchase)
        {
            return Task.Run(() =>
            {
                if (purchase.Date == default) purchase.Date = DateTime.Now;
                _purchases.Insert(purchase);
            });
        }

        // Дополнительные CRUD методы можно добавить при необходимости
    }
}

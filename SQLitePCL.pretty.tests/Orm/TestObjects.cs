using System;
using SQLitePCL.pretty.Orm;
using SQLitePCL.pretty.Orm.Attributes;

namespace SQLitePCL.pretty.tests
{
    public class Product
    {
        [AutoIncrement, PrimaryKey]
        public int? Id { get; set; }

        public string Name { get; set; }

        public decimal Price { get; set; }

        public uint TotalSales { get; set; }
    }

    public class Order
    {
        [AutoIncrement, PrimaryKey]
        public int? Id { get; set; }

        public DateTime PlacedTime { get; set; }
    }

    public class OrderHistory
    {
        [AutoIncrement, PrimaryKey]
        public int? Id { get; set; }

        public int OrderId { get; set; }

        public DateTime Time { get; set; }

        public string Comment { get; set; }
    }

    public class OrderLine
    {
        [AutoIncrement, PrimaryKey]
        public int? Id { get; set; }

        [Indexed("IX_OrderProduct", 1)]
        public int OrderId { get; set; }

        [Indexed("IX_OrderProduct", 2)]
        public int ProductId { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public OrderLineStatus Status { get; set; }
    }

    public enum OrderLineStatus
    {
        Placed = 1,

        Shipped = 100
    }
}


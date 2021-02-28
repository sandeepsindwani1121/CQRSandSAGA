using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace repositorypattern
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }
    }

    public interface IRepositery<T>
    {
        bool Add(T obj); 
        bool Update(T obj,int id);
        List<T> GetQuery();

    }

    public class BaseRepositery<T> : IRepositery<T> where T : class
    {
        DbContext _dbContext;
        DbSet<T> _dbSet;
        public BaseRepositery(DbContext dbContext)
        {
            this._dbContext = dbContext;
            this._dbSet = _dbContext.Set<T>();
        }

        public bool Add(T obj)
        {
            this._dbSet.Add(obj);
            return true;
        }

        public List<T> GetQuery()
        {
            return this._dbSet.ToList(); ;
        }

        public bool Update(T obj, int id)
        {
            this._dbSet.Update(obj);
            return true;
        }
    }

    public class Customer
    {
        public string Name { get; set; }
    }
    public class Supplier
    {
        public string Name { get; set; }
    }
    public interface IUnitOfWork
    {
        IRepositery<Customer> Customers { get; }
        IRepositery<Supplier> Suppliers { get; }
    }


    public class UnitOfWork : IUnitOfWork
    {
        DbContext _dbContext;
        BaseRepositery<Customer> _customer;
        BaseRepositery<Supplier> _supplier;
        public UnitOfWork(DbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public IRepositery<Customer> Customers
        {
            get
            {
                return _customer ??
                    (_customer = new BaseRepositery<Customer>(_dbContext));
            }
        }

        public IRepositery<Supplier> Suppliers
        {
            get
            {
                return _supplier ??
                    (_supplier = new BaseRepositery<Supplier>(_dbContext));
            }
        }
    }

}

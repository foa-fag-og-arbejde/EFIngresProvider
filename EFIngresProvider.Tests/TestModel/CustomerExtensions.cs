using System;

namespace EFIngresProvider.Tests.TestModel
{
    partial class Customer : IEquatable<Customer>
    {
        public Customer()
        {
        }

        public Customer(Customer customer)
        {
            CustomerID = customer.CustomerID;
            CompanyName = customer.CompanyName;
            ContactName = customer.ContactName;
            ContactTitle = customer.ContactTitle;
            Address = customer.Address;
            City = customer.City;
            Region = customer.Region;
            PostalCode = customer.PostalCode;
            Country = customer.Country;
            Phone = customer.Phone;
            Fax = customer.Fax;
        }

        public bool Equals(Customer other)
        {
            return other.CustomerID == CustomerID 
                && other.CompanyName == CompanyName 
                && other.ContactName == ContactName 
                && other.ContactTitle == ContactTitle 
                && other.Address == Address 
                && other.City == City 
                && other.Region == Region 
                && other.PostalCode == PostalCode 
                && other.Country == Country 
                && other.Phone == Phone 
                && other.Fax == Fax;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Customer);
        }

        private int GetHashCode(string value)
        {
            return value == null ? 0 : value.GetHashCode();
        }

        public override int GetHashCode()
        {
            return GetHashCode(CustomerID)
                 ^ GetHashCode(CompanyName)
                 ^ GetHashCode(ContactName)
                 ^ GetHashCode(ContactTitle)
                 ^ GetHashCode(Address)
                 ^ GetHashCode(City)
                 ^ GetHashCode(Region)
                 ^ GetHashCode(PostalCode)
                 ^ GetHashCode(Country)
                 ^ GetHashCode(Phone)
                 ^ GetHashCode(Fax);
        }
    }
}

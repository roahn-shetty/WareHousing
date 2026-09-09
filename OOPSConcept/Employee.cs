using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OOPSConcept
{
    public abstract class Employee
    {
        public int Id { get; init; }

        public string Name { get; private set; }  //Other classes can read: employee.Name && but cannot do: employee.Name = "ABC"; // Instead they must use: employee.ChangeName("ABC");

        private decimal _salary;

        public decimal Salary
        {
            get => _salary;

            protected set
            {
                if (value < 0)
                    throw new ArgumentException("Salary cannot be negative.");

                _salary = value;
            }
        }

        protected Employee( int id, string name, decimal salary)
        {
            Id = id;
            Name = name;
            Salary = salary;
        }

        public void ChangeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name is required.");

            Name = name;
        }
    }
}

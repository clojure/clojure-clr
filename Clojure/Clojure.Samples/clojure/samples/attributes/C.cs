using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace dm
{
    // stolen mostly from the MS docs on System.Attribute

    public enum Pet
    {
        Unknown,
        Dog,
        Cat,
        Bird
    }

    public class PetTypeAttribute : Attribute
    {
        private Pet _pet;

        protected Pet ThePet
        {
            get { return _pet; }
            set { _pet = value; }
        }

        public PetTypeAttribute(Pet pet)
        {
            _pet = pet;
        }

        public override string ToString()
        {
            return String.Format("<Pet {0}>", _pet.ToString());
        }
       
    }


}

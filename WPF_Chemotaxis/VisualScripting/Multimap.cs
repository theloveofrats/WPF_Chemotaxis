using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_Chemotaxis.VisualScripting
{
    // Mapping functions for a many-to-one relationship table.
    public class Multimap<T, U>
    {
        // In my case UI element T to model element (each UI element specifies ONE model element)
        private Dictionary<T, U> _forewardmap;
        //In my case model element U to list of UI elements List<T>;
        private Dictionary<U, HashSet<T>> _backwardmap;

        public Multimap()
        {
            _forewardmap = new();
            _backwardmap = new();
        }

        // returns false if the mapping already exists.
        public bool TryAdd(T ofMany, U singular)
        {
            if (Contains(ofMany, singular)) return false;
            
            if (_backwardmap.ContainsKey(singular))
            {
                _backwardmap[singular].Add(ofMany);
                _forewardmap.Add(ofMany, singular);
            }
            else
            {
                _backwardmap.Add(singular, new() {ofMany});
                _forewardmap.Add(ofMany, singular);
            }
            return true;
        }

        public bool Contains(T ofMany)
        {
            return _forewardmap.ContainsKey(ofMany);
        }
        public bool Contains(U singular)
        {
            return _backwardmap.ContainsKey(singular);
        }
        public bool Contains(T ofMany, U singular)
        {
            U outValue;
            if(_forewardmap.TryGetValue(ofMany, out outValue))
            {
                return outValue.Equals(singular);
            }
            else return false;
        }
        
        /// <summary>
        /// Removes T, out parameter feeds out associated U, which may still have a dictionary presence. Returns false if T not present or not actually removed. 
        /// </summary>
        /// <param name="ofMany"></param>
        /// <param name="associatedSingleValue"></param>
        /// <returns></returns>
        public bool Remove(T ofMany, out U associatedSingleValue)
        {
            bool removed = false;
            // If T is an extant foreward key
            if(_forewardmap.Remove(ofMany, out associatedSingleValue)){
                
                var items = _backwardmap[associatedSingleValue];
                items.Remove(ofMany);
                if (_backwardmap[associatedSingleValue].Count() == 0)
                {
                    HashSet<T> empty;
                    _backwardmap.Remove(associatedSingleValue, out empty);
                }
                removed = true;
            }
            return removed;
        }

        /// <summary>
        /// Removes U, out parameter feeds out list of removed T. Returns false if U not present or not actually removed.  
        /// </summary>
        /// <param name="ofMany"></param>
        /// <param name="associatedSingleValue"></param>
        /// <returns></returns>
        public bool Remove(U singular, out HashSet<T> associatedItemsList)
        {
            bool removed = false;
            // If T is an extant foreward key
            if (_backwardmap.Remove(singular, out associatedItemsList))
            {
                foreach (T item in associatedItemsList)
                {
                    _forewardmap.Remove(item);
                }
                removed = true;
            }
            return removed;
        }

        public bool TryGetValue(T ofMany, out U singularOutValue)
        {
            return (_forewardmap.TryGetValue(ofMany, out singularOutValue));
        }

        public bool TryGetValues(U singularKey, out List<T> recoveredValues)
        {
            HashSet<T> outValues;
            if (_backwardmap.TryGetValue(singularKey, out outValues))
            {
                recoveredValues = outValues.ToList();
                return true;
            }
            else
            {
                recoveredValues = new List<T>();
                return false;
            }
        }

        public int CountManyToOne()
        {
            return _forewardmap.Count;
        }
        public int CountOneToMany()
        {
            return _backwardmap.Count;
        }
        public List<U> SingularItemsList()
        {
            return _backwardmap.Keys.ToList();
        }
        public List<T> MultipleItemsList()
        {
            return _forewardmap.Keys.ToList();
        }

        public void Clear()
        {
            _forewardmap.Clear();
            _backwardmap.Clear();
        }
    }
}

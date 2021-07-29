using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF_Chemotaxis.Model;
using WPF_Chemotaxis.UX;

namespace WPF_Chemotaxis
{
    class ExtantReferenceResolver : IReferenceResolver
    {
        private readonly IDictionary<string, ILinkable> _stringToRef = new Dictionary<string, ILinkable>();
        private readonly IDictionary<ILinkable, string> _refToString = new Dictionary<ILinkable, string>();

        private IList<ILinkable> extantElements;
        private int refCount = 0;

        public ExtantReferenceResolver(ICollection<ILinkable> extant)
        {
            this.extantElements = new List<ILinkable>(extant);
        }

        public object ResolveReference(object context, string reference)
        {
            ILinkable link;
            _stringToRef.TryGetValue(reference, out link);

            ILinkable alt = extantElements.Where(ext => ext != link && ext.Name==link.Name && ext.GetType()==link.GetType()).FirstOrDefault();

            if (alt != null)
            {
                System.Diagnostics.Debug.Print(string.Format("Substituting extant {0} named {1}", alt.GetType().Name, alt.Name));
                return alt;
            }
            return link;

        }

        public string GetReference(object context, object value)
        {
            ILinkable link = (ILinkable) value;

            string result = null;
            if(!_refToString.TryGetValue(link, out result))
            {
                result = (++refCount).ToString(CultureInfo.InvariantCulture);
                _refToString.Add(link, result);
                _stringToRef.Add(result, link);
            }


            return result;
        }

        public bool IsReferenced(object context, object value)
        {
            ILinkable link = (ILinkable) value;

            return _refToString.ContainsKey(link);
        }

        public void AddReference(object context, string reference, object value)
        {
            if (!_stringToRef.ContainsKey(reference))
            {
                _stringToRef.Add(reference, (ILinkable) value);
                _refToString.Add((ILinkable)value, reference);
            }
        }
    }
}

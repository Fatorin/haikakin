using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Models.ECPayModel
{
    public class ECPaymentPostModel
    {
        public string PostValue { get; private set; }
        private SortedDictionary<string, string> PostCollection { get; set; }

        public ECPaymentPostModel(ECPaymentModel model)
        {
            PostCollection = new SortedDictionary<string, string>();
            SetParameter(model);
            PostValue = DictionaryToParamter();
        }
        public ECPaymentPostModel(ECPaymentResponseModel model)
        {
            model.CheckMacValue = null;
            PostCollection = new SortedDictionary<string, string>();
            SetParameter(model);
            PostValue = DictionaryToParamter();
        }

        private string DictionaryToParamter()
        {
            return string.Join("&", PostCollection.Select(p => p.Key + "=" + p.Value).ToArray());
        }

        private void SetParameter(object target)
        {
            object value;
            foreach (var prop in target.GetType().GetProperties())
            {
                value = prop.GetValue(target, null);
                if (null != value)
                {
                    this.PostCollection[prop.Name] = value.ToString();
                }
            }
        }
    }
}

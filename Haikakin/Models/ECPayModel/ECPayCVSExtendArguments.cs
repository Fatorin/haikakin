﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Models.ECPayModel
{
    public class ECPayCVSExtendArguments
    {
        /// <summary>
        /// 超商繳費截止時間
        /// </summary>
        public int StoreExpireDate { get; set; }
        /// <summary>
        /// 交易描述1
        /// </summary>
        public string Desc_1 { get; set; }

        /// <summary>
        /// 交易描述2
        /// </summary>
        public string Desc_2 { get; set; }

        /// <summary>
        /// 交易描述3
        /// </summary>
        public string Desc_3 { get; set; }

        /// <summary>
        /// 交易描述4
        /// </summary>
        public string Desc_4 { get; set; }

        /// <summary>
        /// Server 端回傳付款相關資訊
        /// </summary>
        public string PaymentInfoURL { get; set; }
    }
}

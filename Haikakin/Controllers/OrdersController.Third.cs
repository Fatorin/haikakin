using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Web;
using Haikakin.Extension.Services;
using Haikakin.Extension.NewebPayUtil;
using Haikakin.Models;
using Haikakin.Models.Dtos;
using Haikakin.Models.MailModel;
using Haikakin.Models.NewebPay;
using Haikakin.Models.OrderModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using static Haikakin.Models.OrderModel.Order;
using RestSharp;

namespace Haikakin.Controllers
{
    public partial class OrdersController : ControllerBase
    {
        //取號且與藍新建立訂單
        [HttpPost("SendOrderToThird")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(NewebPayBase))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorPack))]
        [Authorize(Roles = "User,Admin")]
        public IActionResult SendOrderToThird([FromBody] OrderSendToThirdDto model)
        {
            if (model == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "請求參數異常" });
            }

            var order = _orderRepo.GetOrder(model.OrderId);
            if (order == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "找不到此訂單" });
            }

            HttpContextGetUserId(out bool result, out int userId);
            if (!result)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "使用者Token異常" });
            }

            var user = _userRepo.GetUser(userId);
            if (user == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "找不到使用者" });
            }

            if (order.UserId != user.UserId)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "不是你的訂單" });
            }

            if (order.OrderStatus != OrderStatusType.NotGetCVSCode)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "訂單已取號、結帳、過期、或是已取消" });
            }

            //紀錄商品名稱
            var itemsNameList = new List<string>();
            var orderInfos = _orderInfoRepo.GetOrderInfosByOrderId(order.OrderId);
            foreach (var orderInfo in orderInfos)
            {
                var product = _productRepo.GetProduct(orderInfo.ProductId);
                itemsNameList.Add($"{product.ProductName} x {orderInfo.Count}");
            }

            //產生藍新訂單

            var newebPayNo =
                $"N{DateTimeOffset.UtcNow.ToOffset(new TimeSpan(8, 0, 0)).ToString("yyyyMMddHHmm")}" +
                $"{order.OrderId.ToString().Substring(4)}";

            var itemsText = new StringBuilder();
            foreach (var item in itemsNameList)
            {
                itemsText.AppendLine(item);
            }

            var thirdData = CreateNewbPayData(newebPayNo, itemsText.ToString(), decimal.ToInt32(order.OrderAmount), user.Email, "CVS");
            //更新訂單的訂單編號
            order.OrderPaySerial = newebPayNo;
            _orderRepo.UpdateOrder(order);

            return StatusCode(201, thirdData);
        }

        /// <summary>
        /// 結束指定訂單，用於第三方的回傳，所以不限定使用者
        /// </summary>
        /// <param name="newebPayModel"></param>
        /// <returns></returns>
        [HttpPost("ReceivePayResult")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(string))]
        [AllowAnonymous]
        public IActionResult ReceivePayResult([FromForm] NewebPayBaseResp newebPayModel)
        {
            if (newebPayModel == null)
            {
                _logger.LogInformation("藍新資料格式異常");
                return BadRequest("資料請求異常");
            }

            if (newebPayModel.Status != "SUCCESS")
            {
                _logger.LogInformation($"交易失敗，錯誤代碼：{newebPayModel.Status}");
            }

            //檢查資料
            if (newebPayModel.MerchantID != _appSettings.NewebPayMerchantID)
            {
                _logger.LogInformation("非本商店訂單");
                return BadRequest("非本商店訂單");
            }

            if (newebPayModel.TradeSha != CryptoUtil.EncryptSHA256($"HashKey={_appSettings.NewebPayHashKey}&{newebPayModel.TradeInfo}&HashIV={_appSettings.NewebPayHashIV}"))
            {
                _logger.LogInformation("非本商店訂單");
                return BadRequest("非本商店訂單");
            }

            var decryptTradeInfo = CryptoUtil.DecryptAESHex(newebPayModel.TradeInfo, _appSettings.NewebPayHashKey, _appSettings.NewebPayHashIV);

            // 取得回傳參數(ex:key1=value1&key2=value2),儲存為NameValueCollection
            NameValueCollection decryptTradeCollection = HttpUtility.ParseQueryString(decryptTradeInfo);
            NewebPayResponse convertModel = LambdaUtil.DictionaryToObject<NewebPayResponse>(decryptTradeCollection.AllKeys.ToDictionary(k => k, k => decryptTradeCollection[k]));

            //依照特店交易編號回傳資料
            var order = _orderRepo.GetOrdersInPaySerial(convertModel.MerchantOrderNo);

            if (order == null)
            {
                _logger.LogInformation($"查無此訂單:{convertModel.MerchantOrderNo}");
                return BadRequest("查無此訂單");
            }

            if (order.OrderStatus == OrderStatusType.Over)
            {
                _logger.LogInformation("特店訂單已結束");
                return BadRequest("訂單已結束");
            }

            if (order.OrderStatus == OrderStatusType.Cancel)
            {
                _logger.LogInformation("特店訂單已取消");
                return BadRequest("特店訂單已取消");
            }

            if (convertModel.Amt != order.OrderAmount)
            {
                _logger.LogInformation("金額不相符");
                return BadRequest("金額不相符");
            }

            //獲得對應訂單並修改
            var user = _userRepo.GetUser(order.UserId);

            //orderInfo不用更新
            var emailOrderInfoList = new List<EmailOrderInfo>();
            foreach (OrderInfo orderInfo in order.OrderInfos)
            {
                //商品名稱與數量
                var emailInfo = new EmailOrderInfo();
                var productName = _productRepo.GetProduct(orderInfo.ProductId).ProductName;
                var orderCount = orderInfo.Count;

                emailInfo.OrderName = $"{productName} x{orderCount}";

                var orderContext = new StringBuilder();

                var productInfos = _productInfoRepo
                    .GetProductInfos()
                    .Where(o => o.OrderInfoId == orderInfo.OrderInfoId)
                    .Where(o => o.ProductId == orderInfo.ProductId)
                    .ToList();

                foreach (ProductInfo productInfo in productInfos)
                {
                    //訂單改成鎖定
                    productInfo.ProductStatus = ProductInfo.ProductStatusEnum.Used;
                    if (!_productInfoRepo.UpdateProductInfo(productInfo))
                    {
                        //系統更新資料異常
                        _logger.LogInformation("特店系統異常_ProductInfo更新異常");
                        return BadRequest("特店系統異常");
                    };
                    var realKey = CryptoUtil.DecryptAESHex(productInfo.Serial, _appSettings.SerialHashKey, _appSettings.SerialHashIV);
                    orderContext.Append($"{realKey}<br>");
                }

                emailInfo.OrderContext = orderContext.ToString();
                emailOrderInfoList.Add(emailInfo);
            }

            order.OrderStatus = OrderStatusType.Over;
            order.OrderLastUpdateTime = DateTime.UtcNow;
            if (!_orderRepo.UpdateOrder(order))
            {
                _logger.LogInformation($"特店系統異常:訂單編號{order.OrderId}");
                return BadRequest("特店系統異常");
            }

            //沒問題就發送序號
            SendMailService service = new SendMailService(_appSettings.MailgunAPIKey);
            EmailOrderFinish mailModel = new EmailOrderFinish { UserName = user.Username, Email = user.Email, OrderId = $"{order.OrderId}", OrderItemList = emailOrderInfoList };
            if (!service.OrderFinishMailBuild(mailModel))
            {
                //信箱系統掛掉
                _logger.LogInformation("序號發送異常");
                return BadRequest("特店系統異常");
            };

            //送發票
            SendReceipt(order.OrderId);

            return Ok();
        }

        /// <summary>
        /// 第三方傳取號結果
        /// </summary>
        /// <param name="newebPayModel"></param>
        /// <returns></returns>
        [HttpPost("RecevieGetCVSCode")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(string))]
        [AllowAnonymous]
        public IActionResult RecevieGetCVSCode([FromForm] NewebPayBaseResp newebPayModel)
        {
            if (newebPayModel == null)
            {
                _logger.LogInformation("藍新取號資料格式異常");
                return BadRequest("資料請求異常");
            }

            if (newebPayModel.Status != "SUCCESS")
            {
                _logger.LogInformation($"取號失敗，錯誤代碼：{newebPayModel.Status}");
            }

            //檢查資料
            if (newebPayModel.MerchantID != _appSettings.NewebPayMerchantID)
            {
                _logger.LogInformation("非本商店訂單");
                return BadRequest("非本商店訂單");
            }

            if (newebPayModel.TradeSha != CryptoUtil.EncryptSHA256($"HashKey={_appSettings.NewebPayHashKey}&{newebPayModel.TradeInfo}&HashIV={_appSettings.NewebPayHashIV}"))
            {
                _logger.LogInformation("非本商店訂單");
                return BadRequest("非本商店訂單");
            }

            var decryptTradeInfo = CryptoUtil.DecryptAESHex(newebPayModel.TradeInfo, _appSettings.NewebPayHashKey, _appSettings.NewebPayHashIV);

            // 取得回傳參數(ex:key1=value1&key2=value2),儲存為NameValueCollection
            NameValueCollection decryptTradeCollection = HttpUtility.ParseQueryString(decryptTradeInfo);
            NewebPayCVSResp convertModel = LambdaUtil.DictionaryToObject<NewebPayCVSResp>(decryptTradeCollection.AllKeys.ToDictionary(k => k, k => decryptTradeCollection[k]));

            //依照特店交易編號回傳資料
            var order = _orderRepo.GetOrdersInPaySerial(convertModel.MerchantOrderNo);

            if (order == null)
            {
                _logger.LogInformation($"查無此訂單:{convertModel.MerchantOrderNo}");
                return BadRequest("查無此訂單");
            }

            if (order.OrderStatus == OrderStatusType.Over)
            {
                _logger.LogInformation("特店訂單已結束");
                return BadRequest("訂單已結束");
            }

            if (order.OrderStatus == OrderStatusType.Cancel)
            {
                _logger.LogInformation("特店訂單已取消");
                return BadRequest("特店訂單已取消");
            }

            if (convertModel.Amt != order.OrderAmount)
            {
                _logger.LogInformation("金額不相符");
                return BadRequest("金額不相符");
            }

            order.OrderCVSCode = convertModel.CodeNo;
            order.OrderPayLimitTime = convertModel.ExpireDate;
            order.OrderThirdPaySerial = convertModel.TradeNo;
            order.OrderStatus = OrderStatusType.HasGotCVSCode;
            if (!_orderRepo.UpdateOrder(order))
            {
                _logger.LogInformation("取號後更新訂單異常");
                return BadRequest("取號後訂單異常");
            }
            _logger.LogInformation("成功接收序號並更新");

            //關閉前一個定時器
            _orderJob.CancelJob(_scheduler, order.OrderId);
            //定時器開啟，時間內沒繳完費自動取消
            _orderJob.StartJob(_scheduler, order.OrderId);

            return Redirect($"https://www.haikakin.com/account/order/{order.OrderId}");
        }

        /// <summary>
        /// 查序號
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("GetOrderCVSCode")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(string))]
        [Authorize(Roles = "User,Admin")]
        public IActionResult GetOrderCVSCode([FromBody] OrderCVSCodeQueryDto model)
        {
            if (model == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "請求參數異常" });
            }

            var order = _orderRepo.GetOrder(model.OrderId);
            if (order == null)
            {
                return NotFound(new ErrorPack { ErrorCode = 1000, ErrorMessage = "查無此訂單" });
            }

            GetCVSData(order, out bool result, out string cvsCode);

            if (!result)
            {
                return NotFound(new ErrorPack { ErrorCode = 1000, ErrorMessage = "找不到對應的序號" });
            }

            return Ok(cvsCode);
        }

        private void GetCVSData(Order order, out bool result, out string cvsCode)
        {
            string merchantID = _appSettings.NewebPayMerchantID;
            string merchantOrderNo = order.OrderPaySerial;
            int amt = int.Parse($"{order.OrderAmount}");

            string checkValue = CryptoUtil.EncryptSHA256(
                $"IV={_appSettings.NewebPayHashIV}&" +
                $"Amt={amt}&" +
                $"MerchantID={merchantID}&" +
                $"MerchantOrderNo={merchantOrderNo}&" +
                $"Key={_appSettings.NewebPayHashKey}");

            RestClient client = new RestClient();
            client.BaseUrl = new Uri("https://ccore.newebpay.com/API/QueryTradeInfo");
            RestRequest request = new RestRequest();
            request.AddParameter("MerchantID", merchantID);
            request.AddParameter("Version", "1.2");
            request.AddParameter("RespondType", "String");
            request.AddParameter("CheckValue", checkValue);
            request.AddParameter("TimeStamp", $"{DateTimeOffset.UtcNow.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeMilliseconds()}");
            request.AddParameter("MerchantOrderNo", merchantOrderNo);
            request.AddParameter("Amt", amt);
            request.Method = Method.POST;
            var response = client.Execute(request);

            NameValueCollection decryptTradeCollection = HttpUtility.ParseQueryString(response.Content);
            NewebPayQueryResp convertModel = LambdaUtil.DictionaryToObject<NewebPayQueryResp>(decryptTradeCollection.AllKeys.ToDictionary(k => k, k => decryptTradeCollection[k]));

            string checkValueResp = CryptoUtil.EncryptSHA256(
                $"HashIV={_appSettings.NewebPayHashIV}&" +
                $"Amt={convertModel.Amt}&" +
                $"MerchantID={merchantID}&" +
                $"MerchantOrderNo={merchantOrderNo}&" +
                $"TradeNo={convertModel.TradeNo}&" +
                $"HashKey={_appSettings.NewebPayHashKey}");

            if (convertModel.CheckCode != checkValueResp)
            {
                result = false;
                cvsCode = "";

            }
            else
            {
                result = true;
                cvsCode = convertModel.PayInfo;
            };
        }

        private NewebPayBase CreateNewbPayData(string ordernumber, string orderItems, int amount, string userEmail, string payType)
        {
            // 目前時間轉換 +08:00, 防止傳入時間或Server時間時區不同造成錯誤
            DateTimeOffset taipeiStandardTimeOffset = DateTimeOffset.Now.ToOffset(new TimeSpan(8, 0, 0));
            // 預設參數
            var version = "1.5";
            var returnURL = "https://www.haikakin.com/account/order";
            var notifyURL = "https://www.haikakin.com/api/v1/Orders/ReceivePayResult";
            var customerURL = "https://www.haikakin.com/api/v1/Orders/RecevieGetCVSCode";

            NewebPayRequest newebPayRequest = new NewebPayRequest()
            {
                // * 商店代號
                MerchantID = _appSettings.NewebPayMerchantID,
                // * 回傳格式
                RespondType = "String",
                // * TimeStamp
                TimeStamp = taipeiStandardTimeOffset.ToUnixTimeSeconds().ToString(),
                // * 串接程式版本
                Version = version,
                // * 商店訂單編號
                MerchantOrderNo = ordernumber,
                // * 訂單金額
                Amt = amount,
                // * 商品資訊
                ItemDesc = orderItems,
                // 繳費有效期限(適用於非即時交易)
                ExpireDate = null,
                // 支付完成 返回商店網址
                ReturnURL = returnURL,
                // 支付通知網址
                NotifyURL = notifyURL,
                // 商店取號網址
                CustomerURL = customerURL,
                // 支付取消 返回商店網址
                ClientBackURL = returnURL,
                // * 付款人電子信箱
                Email = userEmail,
                // 付款人電子信箱 是否開放修改(1=可修改 0=不可修改)
                EmailModify = 0,
                // 商店備註
                OrderComment = null,
                // 信用卡 一次付清啟用(1=啟用、0或者未有此參數=不啟用)
                CREDIT = null,
                // WEBATM啟用(1=啟用、0或者未有此參數，即代表不開啟)
                WEBATM = null,
                // ATM 轉帳啟用(1=啟用、0或者未有此參數，即代表不開啟)
                VACC = null,
                // 超商代碼繳費啟用(1=啟用、0或者未有此參數，即代表不開啟)(當該筆訂單金額小於 30 元或超過 2 萬元時，即使此參數設定為啟用，MPG 付款頁面仍不會顯示此支付方式選項。)
                CVS = null,
                // 超商條碼繳費啟用(1=啟用、0或者未有此參數，即代表不開啟)(當該筆訂單金額小於 20 元或超過 4 萬元時，即使此參數設定為啟用，MPG 付款頁面仍不會顯示此支付方式選項。)
                BARCODE = null
            };

            if (string.Equals(payType, "CREDIT"))
            {
                newebPayRequest.CREDIT = 1;
            }
            else if (string.Equals(payType, "WEBATM"))
            {
                newebPayRequest.WEBATM = 1;
            }
            else if (string.Equals(payType, "VACC"))
            {
                // 設定繳費截止日期
                newebPayRequest.ExpireDate = taipeiStandardTimeOffset.AddDays(1).ToString("yyyyMMdd");
                newebPayRequest.VACC = 1;
            }
            else if (string.Equals(payType, "CVS"))
            {
                // 設定繳費截止日期
                newebPayRequest.ExpireDate = taipeiStandardTimeOffset.AddHours(1).ToString("yyyyMMdd");
                newebPayRequest.CVS = 1;
            }
            else if (string.Equals(payType, "BARCODE"))
            {
                // 設定繳費截止日期
                newebPayRequest.ExpireDate = taipeiStandardTimeOffset.AddHours(1).ToString("yyyyMMdd");
                newebPayRequest.BARCODE = 1;
            }

            var inputModel = new NewebPayBase
            {
                MerchantID = _appSettings.NewebPayMerchantID,
                Version = version
            };

            // 將model 轉換為List<KeyValuePair<string, string>>, null值不轉
            List<KeyValuePair<string, string>> tradeData = LambdaUtil.ModelToKeyValuePairList<NewebPayRequest>(newebPayRequest);
            // 將List<KeyValuePair<string, string>> 轉換為 key1=Value1&key2=Value2&key3=Value3...
            var tradeQueryPara = string.Join("&", tradeData.Select(x => $"{x.Key}={x.Value}"));
            // AES 加密
            inputModel.TradeInfo = CryptoUtil.EncryptAESHex(tradeQueryPara, _appSettings.NewebPayHashKey, _appSettings.NewebPayHashIV);
            // SHA256 加密
            inputModel.TradeSha = CryptoUtil.EncryptSHA256($"HashKey={_appSettings.NewebPayHashKey}&{inputModel.TradeInfo}&HashIV={_appSettings.NewebPayHashIV}");

            return inputModel;
        }

        [HttpGet("TestReceip")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(string))]
        [AllowAnonymous]
        public IActionResult TestReceip(int orderId)
        {
            if (SendReceipt(orderId))
            {
                return BadRequest("錯誤");
            }

            return Ok();
        }


        private bool SendReceipt(int orderId)
        {
            string apiUrl = "https://cinv.ezpay.com.tw/Api/invoice_issue";
            //正式平台 https://inv.ezpay.com.tw/Api/invoice_issue

            var order = _orderRepo.GetOrder(orderId);
            var user = _userRepo.GetUser(order.UserId);
            //商品名稱與內容
            //紀錄商品名稱
            int buyCount = 0;
            decimal amt = 0;
            decimal taxRate = 5;
            var orderInfos = _orderInfoRepo.GetOrderInfosByOrderId(order.OrderId);
            foreach (var orderInfo in orderInfos)
            {
                Product product = _productRepo.GetProduct(orderInfo.ProductId);
                buyCount += orderInfo.Count;
                amt += product.Price * orderInfo.Count * product.AgentFeePercent / 100;
            }
            //總額先無條件進位
            int amtTotal = decimal.ToInt32(Math.Ceiling(amt));
            //稅額
            int taxAmt = decimal.ToInt32(Math.Ceiling(amtTotal * taxRate));
            //銷售額
            int sellAmt = amtTotal - taxAmt;
            //總額            
            NewebPayReceipt newebPayReceipt = new NewebPayReceipt()
            {
                RespondType = "String",
                Version = "1.4",
                TimeStamp = $"{DateTimeOffset.UtcNow.ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeMilliseconds()}",
                //ezPay平台交易序號
                TransNum = null,
                //MerchantOrderNo = order.OrderPaySerial,
                MerchantOrderNo = "N1234567089",
                //開發票方式
                Status = "1",
                CreateStatusTime = null,
                //發票種類，開給用戶
                Category = "B2C",
                //買受人名稱
                BuyerName = user.Username,
                BuyerUBN = null,
                BuyerAddress = null,
                BuyerEmail = user.Email,
                CarrierType = null,
                CarrierNum = null,
                LoveCode = null,
                PrintFlag = "Y",
                TaxType = "1",
                TaxRate = (float)taxRate,
                CustomsClearance = null,
                //銷售額
                Amt = amtTotal,
                AmtSales = null,
                AmtZero = null,
                AmtFree = null,
                //稅額
                TaxAmt = taxAmt,
                //發票金額
                TotalAmt = amtTotal,
                //商品名稱
                ItemName = "代購",
                ItemCount = 1,
                ItemUnit = "次",
                ItemPrice = amtTotal,
                ItemAmt = amtTotal,
                ItemTaxType = null,
                Comment = null,
            };

            //如果是手機載具
            if (order.CarrierType == CarrierTypeEnum.Phone)
            {
                newebPayReceipt.CarrierType = "0";
                newebPayReceipt.CarrierNum = order.CarrierNum;
            }

            //如果是自然人憑證
            if (order.CarrierType == CarrierTypeEnum.Moica)
            {
                newebPayReceipt.CarrierType = "1";
                newebPayReceipt.CarrierNum = order.CarrierNum;
            }

            //如果是愛心捐
            if (order.CarrierType == CarrierTypeEnum.Love)
            {
                newebPayReceipt.LoveCode = int.Parse(order.CarrierNum);
            }

            // 將model 轉換為List<KeyValuePair<string, string>>, null值不轉
            List<KeyValuePair<string, string>> tradeData = LambdaUtil.ModelToKeyValuePairList<NewebPayReceipt>(newebPayReceipt);
            // 將List<KeyValuePair<string, string>> 轉換為 key1=Value1&key2=Value2&key3=Value3...
            string tradeQueryPara = string.Join("&", tradeData.Select(x => $"{x.Key}={x.Value}"));
            NewebPayReceiptBase newebPayReceiptBase = new NewebPayReceiptBase()
            {
                MerchantID_ = _appSettings.EzPayMerchantID,
                PostData_ = CryptoUtil.EncryptAES256(tradeQueryPara, _appSettings.EzPayHashKey, _appSettings.EzPayHashIV),
            };

            RestClient client = new RestClient();
            client.BaseUrl = new Uri(apiUrl);
            RestRequest request = new RestRequest();
            request.AddParameter("MerchantID_", newebPayReceiptBase.MerchantID_);
            request.AddParameter("PostData_", newebPayReceiptBase.PostData_);
            request.Method = Method.POST;
            var response = client.Execute(request);

            //轉換回傳資料
            NameValueCollection decryptTradeCollection = HttpUtility.ParseQueryString(response.Content);
            NewebPayReceiptBaseResp convertModel = LambdaUtil.DictionaryToObject<NewebPayReceiptBaseResp>(decryptTradeCollection.AllKeys.ToDictionary(k => k, k => decryptTradeCollection[k]));

            if (convertModel.Status != "SUCCESS")
            {
                _logger.LogInformation($"開立發票失敗：{convertModel.Status}");
                return false;
            }

            string checkValue = CryptoUtil.EncryptSHA256(
                $"IV={_appSettings.NewebPayHashIV}&" +
                $"InvoiceTransNo={convertModel.InvoiceTransNo}&" +
                $"MerchantID={convertModel.MerchantID}&" +
                $"MerchantOrderNo={convertModel.MerchantOrderNo}&" +
                $"RandomNum={convertModel.RandomNum}&" +
                $"TotalAmt={convertModel.TotalAmt}&" +
                $"Key={_appSettings.NewebPayHashKey}");

            if (convertModel.CheckCode != checkValue)
            {
                _logger.LogInformation("回傳檢查碼異常，無法開立發票");
                return false;
            }

            return true;
        }
    }
}

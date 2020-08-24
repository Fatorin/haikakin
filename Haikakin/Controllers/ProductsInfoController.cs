using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Haikakin.Extension;
using Haikakin.Extension.NewebPayUtil;
using Haikakin.Models;
using Haikakin.Models.Dtos;
using Haikakin.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Haikakin.Controllers
{
    [Authorize]
    [Route("api/v{version:apiVersion}/ProductsInfos")]
    [ApiController]
    public class ProductsInfoController : ControllerBase
    {
        private IProductInfoRepository _productInfoRepo;
        private IProductRepository _productRepo;
        private readonly AppSettings _appSettings;

        public ProductsInfoController(IProductInfoRepository productInfoRepo, IProductRepository productRepo, IOptions<AppSettings> appSettings)
        {
            _productInfoRepo = productInfoRepo;
            _productRepo = productRepo;
            _appSettings = appSettings.Value;
        }

        /// <summary>
        /// 獲得全部產品序號，Admin限定
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetProductInfos")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ProductInfo>))]
        [Authorize(Roles = "Admin")]
        public IActionResult GetProductInfos()
        {
            var productInfoList = _productInfoRepo.GetProductInfos();
            foreach (var productInfo in productInfoList)
            {
                //解密
                var key = CryptoUtil.DecryptAESHex(productInfo.Serial, _appSettings.SerialHashKey, _appSettings.SerialHashIV);
                //加密
                productInfo.Serial = key.SerialEncrypt();
            }

            return Ok(productInfoList);
        }

        /// <summary>
        /// 獲得單一產品序號，Admin限定
        /// </summary>
        /// <param name="productInfoId"></param>
        /// <returns></returns>
        [HttpPost("GetProductInfo")]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductInfo))]
        [Authorize(Roles = "Admin")]
        public IActionResult GetProductInfo(int productInfoId)
        {
            var productInfo = _productInfoRepo.GetProductInfo(productInfoId);
            //解密
            var key = CryptoUtil.DecryptAESHex(productInfo.Serial, _appSettings.SerialHashKey, _appSettings.SerialHashIV);
            //加密
            productInfo.Serial = key.SerialEncrypt();
            if (productInfo.Serial == null)
            {
                return StatusCode(500, new ErrorPack { ErrorCode = 1000, ErrorMessage = "序號解密錯誤" });
            }

            if (productInfo == null)
            {
                return NotFound(new ErrorPack { ErrorCode = 1000, ErrorMessage = "不存在的項次" });
            }

            return Ok(productInfo);
        }

        /// <summary>
        /// 上傳產品序號用表單上傳，Admin限定
        /// </summary>
        /// <param name="productInfoFile"></param>
        /// <returns></returns>
        [HttpPost("CreateProductInfo")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Authorize(Roles = "Admin")]
        public IActionResult CreateProductInfo([FromForm] ProductInfoUploadDto productInfoFile)
        {
            if (productInfoFile == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "接收資料異常" });
            }

            if (!_productRepo.ProductExists(productInfoFile.ProductId))
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "不存在的商品項目" });
            }

            var count = 0;
            var failTimes = 0;
            var file = productInfoFile.SerialFile;
            if (file == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "請確認是否有上傳檔案" });
            }

            if (file.Length > 0)
            {
                StreamReader reader = new StreamReader(file.OpenReadStream());
                while (reader.Peek() >= 0)
                {
                    var serial = reader.ReadLine();
                    if (serial == null || serial.Trim().Length < 8)
                    {
                        failTimes++;
                        continue;
                    }

                    var encryptSerial = CryptoUtil.EncryptAESHex(serial.Trim(), _appSettings.SerialHashKey, _appSettings.SerialHashIV);
                    var productInfo = new ProductInfo()
                    {
                        Serial = encryptSerial,
                        LastUpdateTime = DateTime.UtcNow,
                        ProductStatus = ProductInfo.ProductStatusEnum.NotUse,
                        ProductId = productInfoFile.ProductId,
                        PrimeCost = productInfoFile.PrimeCost
                    };
                    //有重複序號會自己跳過
                    if (_productInfoRepo.ProductInfoSerialExists(productInfoFile.ProductId, encryptSerial))
                    {
                        failTimes++;
                        continue;
                    }
                    //沒有就會新增
                    _productInfoRepo.CreateProductInfo(productInfo);
                    count++;
                }
                reader.Close();
                reader.Dispose();
            }

            return Ok(new ErrorPack { ErrorCode = 1000, ErrorMessage = $"新增個數:{count}, 失敗個數:{failTimes}" });
        }

        /// <summary>
        /// 更新特定產品序號，Admin限定
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPatch("UpdateProductInfo")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Authorize(Roles = "Admin")]
        public IActionResult UpdateProductInfo(ProductInfoDto model)
        {
            if (model == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "請求資料異常" });
            }

            var productInfo = _productInfoRepo.GetProductInfo(model.ProductInfoId);

            if (productInfo == null)
            {
                return NotFound(new ErrorPack { ErrorCode = 1000, ErrorMessage = "不存在的項目" });
            }

            if (string.IsNullOrEmpty(model.Serial))
            {
                var encryptSerial = CryptoUtil.EncryptAESHex(model.Serial, _appSettings.SerialHashKey, _appSettings.SerialHashIV);
                productInfo.Serial = encryptSerial;
            }

            productInfo.ProductStatus = model.ProductStatus;
            productInfo.PrimeCost = model.PrimeCost;
            productInfo.LastUpdateTime = DateTime.UtcNow;

            if (!_productInfoRepo.UpdateProductInfo(productInfo))
            {
                return StatusCode(500, new ErrorPack { ErrorCode = 1000, ErrorMessage = "更新錯誤" });
            }

            return Ok();
        }

        /// <summary>
        /// 刪除特定產品序號，Admin限定
        /// </summary>
        /// <param name="productInfoId"></param>
        /// <returns></returns>
        [HttpDelete("DeleteProductInfo")]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteProductInfo(int productInfoId)
        {
            var obj = _productInfoRepo.GetProductInfo(productInfoId);

            if (obj == null)
            {
                return NotFound(new ErrorPack { ErrorCode = 1000, ErrorMessage = "找不到對應的序號" });
            }

            if (obj.ProductStatus != ProductInfo.ProductStatusEnum.NotUse)
            {
                return NotFound(new ErrorPack { ErrorCode = 1000, ErrorMessage = "此序號已鎖定或已使用" });
            }

            if (!_productInfoRepo.DeleteProductInfo(obj.ProductInfoId))
            {
                return StatusCode(500, new ErrorPack { ErrorCode = 1000, ErrorMessage = "刪除序號失敗" });
            }

            return Ok();
        }
    }
}

﻿using Haikakin.Repository.IRepository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Haikakin.Models.Order;

namespace Haikakin.Models.OrderScheduler
{
    public class OrderJob : IJob
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;

        public OrderJob(ILogger<OrderJob> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }
        public Task Execute(IJobExecutionContext context)
        {
            var orderId = context.JobDetail.JobDataMap.GetInt("orderId");

            using (var scope = _serviceProvider.CreateScope())
            {
                var _userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                var _orderRepo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
                var _productInfoRepo = scope.ServiceProvider.GetRequiredService<IProductInfoRepository>();
                // do something with context

                var orderObj = _orderRepo.GetOrder(orderId);
                _logger.LogInformation($"orderId={orderId}");
                if (orderObj == null)
                {
                    return Task.CompletedTask;
                }

                if (orderObj.OrderStatus == OrderStatusType.Over)
                {
                    return Task.CompletedTask;
                }

                if (orderObj.OrderStatus == OrderStatusType.Cancel)
                {
                    return Task.CompletedTask;
                }

                //更新庫存 解除已使用
                orderObj.OrderStatus = OrderStatusType.Cancel;
                orderObj.OrderLastUpdateTime = DateTime.UtcNow;
                _orderRepo.UpdateOrder(orderObj);
                foreach (OrderInfo orderInfo in orderObj.OrderInfos)
                {
                    var productInfos = _productInfoRepo
                        .GetProductInfos()
                        .Where(o => o.OrderInfoId == orderInfo.OrderInfoId)
                        .Where(o => o.ProductId == orderInfo.ProductId)
                        .ToList();

                    foreach (ProductInfo productInfo in productInfos)
                    {
                        productInfo.OrderInfoId = null;
                        productInfo.LastUpdateTime = DateTime.UtcNow;
                        productInfo.ProductStatus = ProductInfo.ProductStatusEnum.NotUse;
                        //更新庫存寫在UpdateProductInfo裡面
                        _productInfoRepo.UpdateProductInfo(productInfo);
                    }
                }

                //計算棄單次數，如果超過就吃BAN
                var user = _userRepo.GetUser(orderObj.UserId);
                user.CancelTimes++;
                if (user.CancelTimes >= 3)
                {
                    user.CheckBan = true;
                }
                _userRepo.UpdateUser(user);
            }
            _logger.LogInformation("A order cancel!");

            return Task.CompletedTask;
        }
        public Task StartJob(IScheduler scheduler, int orderId)
        {
            //use JobBuilder to Create a jobDetail
            var jobDetails = JobBuilder
            .Create<OrderJob>()
            .WithIdentity($"OrderJob{orderId}")
            .WithDescription("A order time up")
            .UsingJobData("orderId", orderId)
            .Build();
            var trigger = TriggerBuilder
            .Create()
            .StartAt(DateTimeOffset.Now.AddHours(1))
            .Build();
            //call the scheduler.ScheduleJob
            return scheduler.ScheduleJob(jobDetails, trigger);
        }
    }
}

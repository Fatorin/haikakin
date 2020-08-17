using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ClosedXML.Excel;
using Haikakin.Models;
using Haikakin.Models.AnnouncementModel;
using Haikakin.Models.Dtos;
using Haikakin.Models.OrderModel;
using Haikakin.Models.UploadValidation;
using Haikakin.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using static Haikakin.Models.User;

namespace Haikakin.Controllers
{
    [Authorize]
    [Route("api/v{version:apiVersion}/Announcements")]
    [ApiController]
    public class AnnouncementController : ControllerBase
    {
        private IAnnouncementRepository _announcementRepo;
        private readonly IMapper _mapper;

        public AnnouncementController(IAnnouncementRepository announcementRepo, IMapper mapper)
        {
            _announcementRepo = announcementRepo;
            _mapper = mapper;
        }

        [HttpGet("GetAnnounments")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Announcement))]
        [AllowAnonymous]
        public IActionResult GetAnnounments()
        {
            var list = _announcementRepo.GetAnnouncements(IAnnouncementRepository.QueryMode.User);

            return Ok(list);
        }

        [HttpGet("GetAnnounmentsForAdmin")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Announcement))]
        [Authorize(Roles = "Admin")]
        public IActionResult GetAnnounmentsForAdmin()
        {
            var list = _announcementRepo.GetAnnouncements(IAnnouncementRepository.QueryMode.Admin);

            return Ok(list);
        }

        [HttpPatch("CreateAnnounment")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Authorize(Roles = "Admin")]
        public IActionResult CreateAnnounment([FromBody] AnnouncementCreateDto model)
        {
            if (model == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "資料接收不正確" });
            }

            model.LastUpdateTime = DateTime.UtcNow;
            
            var obj = _mapper.Map<Announcement>(model);

            if (!_announcementRepo.UpdateAnnouncement(obj))
            {
                return StatusCode(500, new ErrorPack { ErrorCode = 1000, ErrorMessage = "更新公告異常" });
            }

            return Ok();
        }

        [HttpPatch("UpdateAnnounment")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [Authorize(Roles = "Admin")]
        public IActionResult UpdateAnnounment([FromBody] Announcement model)
        {
            if (model == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "資料接收不正確" });
            }

            model.LastUpdateTime = DateTime.UtcNow;
            if (!_announcementRepo.UpdateAnnouncement(model))
            {
                return StatusCode(500, new ErrorPack { ErrorCode = 1000, ErrorMessage = "更新公告異常" });
            }

            return NoContent();
        }
    }
}

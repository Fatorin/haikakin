using System;
using System.IO;
using System.Threading.Tasks;
using AutoMapper;
using Haikakin.Models;
using Haikakin.Models.AnnouncementModel;
using Haikakin.Models.Dtos;
using Haikakin.Models.UploadModel;
using Haikakin.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<IActionResult> CreateAnnounment([FromForm] AnnouncementCreateDto model)
        {
            if (model == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "資料接收不正確" });
            }

            var obj = _mapper.Map<Announcement>(model);
            obj.LastUpdateTime = DateTime.UtcNow;
            //有檔案時作更新
            if (model.Image != null)
            {
                var response = await LocalUploadHelper.ImageUpload(model.Image, null).ConfigureAwait(true);

                if (!response.Result)
                {
                    return StatusCode(500, new ErrorPack { ErrorCode = 1000, ErrorMessage = "建立公告異常，上傳圖片出錯" });
                }

                obj.ImageUrl = response.SaveUrl;
            }

            if (!_announcementRepo.CreateAnnouncement(obj))
            {
                return StatusCode(500, new ErrorPack { ErrorCode = 1000, ErrorMessage = "建立公告異常" });
            }

            return Ok();
        }

        [HttpPatch("UpdateAnnounment")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorPack))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateAnnounment([FromForm] AnnouncementUpdateDto model)
        {
            if (model == null)
            {
                return BadRequest(new ErrorPack { ErrorCode = 1000, ErrorMessage = "資料接收不正確" });
            }

            var annObj = _announcementRepo.GetAnnouncement(model.AnnouncementId);
            if (annObj == null)
            {
                return NotFound(new ErrorPack { ErrorCode = 1000, ErrorMessage = "沒有對應的公告" });
            }

            if (model.Image != null)
            {
                var response = await LocalUploadHelper.ImageUpload(model.Image, annObj.ImageUrl).ConfigureAwait(true);

                if (!response.Result)
                {
                    return StatusCode(500, new ErrorPack { ErrorCode = 1000, ErrorMessage = "更新公告失敗，上傳圖片部分異常" });
                }

                annObj.ImageUrl = response.SaveUrl;
            }

            annObj.Title = model.Title;
            annObj.FullContext = model.FullContext;
            annObj.ShortContext = model.ShortContext;
            annObj.IsActive = model.IsActive;
            annObj.LastUpdateTime = DateTime.UtcNow;

            if (!_announcementRepo.UpdateAnnouncement(annObj))
            {
                return StatusCode(500, new ErrorPack { ErrorCode = 1000, ErrorMessage = "更新公告異常" });
            }

            return NoContent();
        }
    }
}

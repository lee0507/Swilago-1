using Borago.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Swilago.Data;
using Swilago.Data.Procedures;
using Swilago.Data.Tables;
using Swilago.Interfaces;
using Swilago.Models;
using System.Diagnostics;
using System.Net;
using System.Reflection;

namespace Swilago.Services
{
    public class RestaurantService : ControllerBase, IRestaurantService
    {
        private readonly IConfiguration _config;
        private readonly PamukkaleContext _context;

        public RestaurantService(IConfiguration config, PamukkaleContext context)
        {
            _config = config;
            _context = context;
        }

        // 접속자 최근 선택항목 가져오기
        public Payload GetRestaurantList(string userEmail)
        {
            Payload payload = new();

            try
            {
                if (!userEmail.Contains('@'))
                    userEmail = "";

                // userEmail을 파라메터에 넣어서 프로시저 실행
                SqlParameter paramUserEmail = new("@UserEmail", userEmail);

                var publicApiTable = _context.Set<PPublicApi>()
                                             .FromSqlRaw(MyQueries.PublicApi, paramUserEmail)
                                             .AsNoTracking();

                List<PublicApiTable> publicApiRowList = new();

                bool rowSelected;
                foreach (var publicApiRow in publicApiTable)
                {
                    rowSelected = (publicApiRow.IsSelected.ToString().Equals("true")) ? true : false;
                    publicApiRowList.Add(new PublicApiTable()
                    {
                        Id = publicApiRow.Id,
                        Text = publicApiRow.Text,
                        IsSelected = rowSelected,
                        Info = publicApiRow.Info
                    });
                }

                payload.Message = JsonConvert.SerializeObject(publicApiRowList, Formatting.Indented);
            }
            catch (Exception ex)
            {
                SetErrorMessages(ref payload, ex);
            }

            return payload;
        }

        // 룰렛 정보 저장
        public IActionResult PostRouletteInfo(string userEmail, string rouletteResult, [FromBody] List<SelResRecord> selResRecords)
        {
            if(!userEmail.Contains('@') || rouletteResult == null || rouletteResult == "" || selResRecords.Count() < 1)
                return BadRequest();

            string resRecord = "";
            foreach (var selResRecord in selResRecords)
            {
                resRecord += selResRecord.Text.ToString() + ",";
            }

            TUserRecord userRecord = new()
            {
                ModifiedDate = DateTime.Now,
                RouletteResult = rouletteResult,
                UserEmail = userEmail,
                ResRecord = resRecord
            };

            _context.UserRecord.Add(userRecord);
            _context.SaveChanges();

            return NoContent();
        }

        // 전체 통계 가져오기
        public Payload GetStatistics()
        {
            Payload payload = new();

            try
            {
                var allStatisticsTable = _context.Set<PAllStatistics>()
                                                 .FromSqlRaw(MyQueries.AllStatistics)
                                                 .AsNoTracking()
                                                 .ToList();

                int addRank = 1;
                foreach (var row in allStatisticsTable)
                {
                    row.ResRank = addRank++;
                }

                payload.Message = JsonConvert.SerializeObject(allStatisticsTable, Formatting.Indented);
            }
            catch (Exception ex)
            {
                SetErrorMessages(ref payload, ex);
            }

            return payload;
        }

        // 접속자 통계 가져오기
        public Payload GetUserStatistics(string userEmail, string searchDate)
        {
            Payload payload = new();

            try
            {/*
                이건 잘 되려나 테스트 7번째 라고 하자

                if (!userEmail.Contains('@'))
                    userEmail = null;

                string startDate = "2021-11-01";
                string endDate = DateTime.Now.ToString("yyyy-MM-dd");

                if (searchDate != "null" || searchDate != null || searchDate != "" || searchDate.Contains('-') || searchDate.Contains(','))
                {
                    string[] splitDate = searchDate.Trim().split(',');
                    startDate = splitDate[0];
                    endDate = splitDate[1];
                }

                SqlParameter paramUserEmail = new("@UserEmail", userEmail);
                SqlParameter paramStartDate = new("@StartDate", startDate);
                SqlParameter paramEndDate = new("@EndDate", endDate);

                var userStatisticsTable = _context.Set<"테이블 이름">()
                                                  .FromSqlRaw(MyQueries.UserStatistics, paramUserEmail, paramStartDate, paramEndDate)
                                                  .AsNoTracking()
                                                  .ToList();

                payload.Message = JsonConvert.SerializeObject(userStatisticsTable, Formatting.Indented);*/
            }
            catch (Exception ex)
            {
                SetErrorMessages(ref payload, ex);
            }

            return payload;
        }

        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------

        // 식당 이름 검색
        public Payload GetRestaurant(string resName)
        {
            Payload payload = new();

            try
            {
                var resRows = _context.Restaurant.AsNoTracking()
                                                 .Where(r => r.ResName.Contains(resName));
                List<Restaurant> foundResList = new();

                foreach (var resRow in resRows)
                {
                    foundResList.Add(new Restaurant()
                    {
                        ResName = resRow.ResName,
                        ResInfo = resRow.ResInfo
                    });
                }

                payload.Message = JsonConvert.SerializeObject(foundResList, Formatting.Indented);
                
                if (resName == "null" || resName == null || resName == "")
                {
                    var resAllRows = _context.Restaurant.AsNoTracking().ToList();
                    payload.Message = JsonConvert.SerializeObject(resAllRows, Formatting.Indented);
                }
                    
            }
            catch (Exception ex)
            {
                SetErrorMessages(ref payload, ex);
            }

            return payload;
        }

        // 식당 추가
        public IActionResult PostRestaurant(string resName, string? resInfo)
        {
            if (resName == "null" || resName == null || resName == "")
                return BadRequest();

            TRestaurant newResRow = new()
            {
                ResName = resName,
                ResInfo = resInfo
            };

            _context.Restaurant.Add(newResRow);
            _context.SaveChanges();

            return NoContent();
        }

        // 식당 삭제
        public IActionResult DeleteRestaurnat(string resName)
        {
            if (resName == "null" || resName == null || resName == "")
                return BadRequest();

            var existsRow = _context.Restaurant.AsNoTracking()
                                               .Where(r => r.ResName == resName)
                                               .SingleOrDefault();

            if (existsRow == null)
                return NotFound();

            _context.Restaurant.Remove(existsRow);
            _context.SaveChanges();

            return NoContent();
        }

        // 식당 정보 수정(식당 이름은 PK, 고유값으로 수정이 안되고 삭제 후 다시 추가로만 변경해야 함)
        public IActionResult PutRestaurnat(string resName, string? resInfo)
        {
            if (resName == "null" || resName == null || resName == "")
                return BadRequest();

            var existsRow = _context.Restaurant.AsNoTracking()
                                               .Where(r => r.ResName == resName)
                                               .SingleOrDefault();

            if (existsRow == null)
                return NotFound();

            existsRow.ResInfo = resInfo;

            if (resName != existsRow.ResName.ToString())
                return BadRequest();

            _context.Restaurant.Update(existsRow);
            _context.SaveChanges();

            return NoContent();
        }
        
        public void SetErrorMessages(ref Payload payload, Exception ex)
        {
            var trace = new StackTrace(ex);
            var assembly = Assembly.GetExecutingAssembly();
            var methodName = trace.GetFrames().Select(f => f.GetMethod()).First(m => m.Module.Assembly == assembly).Name;

            payload.ErrorMessages.Add(new string('-', 10));
            payload.ErrorMessages.Add($"## Exception from {methodName}");
            payload.ErrorMessages.Add($"## Exception Message {ex.Message}");

            
            if (ex.InnerException != null)
                payload.ErrorMessages.Add($"## Inner Exception Message {ex.InnerException.Message}");

            payload.ErrorMessages.Add(new string('-', 10));

            if (ex is WebException)
            {
                HttpWebResponse response = (HttpWebResponse)(ex as WebException).Response;
                payload.StatusCode = response.StatusCode;
            }

            Debug.WriteLine(string.Join(Environment.NewLine, payload.ErrorMessages.ToArray()));
        }
    }
}

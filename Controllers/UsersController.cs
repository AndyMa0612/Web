using Badminton.Models;
using System;
using System.Data;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web.Mvc;

namespace Badminton.Controllers
{
    public class UsersController : Controller
    {
        private DatabaseEntities db = new DatabaseEntities();

        public ActionResult Index()
        {
            return View(db.User.ToList());
        }


        // GET: Users/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        // 若要避免過量張貼攻擊，請啟用您要繫結的特定屬性。
        // 如需詳細資料，請參閱 https://go.microsoft.com/fwlink/?LinkId=317598。
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Student , Password , Phone")] User user)
        {
            if (ModelState.IsValid)
            {
                user.Location = 0;
                user.Time = 0;
                db.User.Add(user);
                db.SaveChanges();
                return RedirectToAction("Index", "Users");
            }

            return View(user);
        }

        // POST: 驗證學號
        [HttpPost]
        public JsonResult ValidateStudent(string student)
        {
            bool exists = db.User.Any(u => u.Student == student);
            return Json(new { isValid = !exists });
        }

        // POST: 驗證電話
        [HttpPost]
        public JsonResult ValidatePhone(string phone)
        {
            bool exists = db.User.Any(u => u.Phone == phone);
            return Json(new { isValid = !exists });
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string Student)
        {
            User user = db.User.Find(Student);
            db.User.Remove(user);
            db.SaveChanges();

            return RedirectToAction("List", "Users");
        }

        // GET: Users/List
        public ActionResult List()
        {
            // 獲取所有帳號資訊
            var users = db.User.ToList();
            return View(users); // 傳遞資料至 View
        }

        [HttpPost]
        public JsonResult Login(string student, string password)
        {
            // 查找用戶
            var user = db.User.FirstOrDefault(u => u.Student == student);

            if (user != null && user.Password == password)
            {
                // 登入成功
                Session["Student"] = user.Student; // 學號
                return Json(new{success = true});
            }

            // 登入失敗
            return Json(new { success = false });
        }

        public ActionResult Site()
        {
            ViewBag.Student = Session["Student"];
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reserve(int Location, string timeSlot)
        {
            var studentId = (string)Session["Student"];
            if (string.IsNullOrEmpty(studentId))
            {
                return RedirectToAction("Index", "Users");
            }

            // Get reserved times for both initial load and after submission
            var reservedTimes = db.User
                .Where(u => u.Location == Location)
                .Select(u => u.Time)
                .ToList();

            ViewBag.Student = Session["Student"];
            ViewBag.SelectedCourt = Location;
            ViewBag.ReservedTimes = reservedTimes;

            // Return view immediately if no timeSlot selected
            if (string.IsNullOrEmpty(timeSlot))
            {
                return View();
            }

            var timeNumber = ConvertTimeSlotToNumber(timeSlot);
            if (reservedTimes.Contains(timeNumber))
            {
                ViewBag.ErrorMessage = "此時段已被預約，請選擇其他時段";
                return View();
            }

            var existingUser = db.User.FirstOrDefault(u => u.Student == studentId);
            if (existingUser == null)
            {
                return RedirectToAction("Index", "Users");
            }

            existingUser.Location = Location;
            existingUser.Time = timeNumber;
            db.SaveChanges();

            return RedirectToAction("Home", "Users");
        }

        // 轉換時間區間為數字
        private int ConvertTimeSlotToNumber(string timeSlot)
        {
            switch (timeSlot)
            {
                case "17:00-18:00":
                    return 5;
                case "18:00-19:00":
                    return 6;
                case "19:00-20:00":
                    return 7;
                case "20:00-21:00":
                    return 8;
                default:
                    return 0;
            }
        }

        // GET: Users/Check
        public ActionResult Home()
        {
            var studentId = (string)Session["Student"];
            if (string.IsNullOrEmpty(studentId))
            {
                return RedirectToAction("Index", "Users");
            }

            var user = db.User.FirstOrDefault(u => u.Student == studentId);
            if (user == null)
            {
                return RedirectToAction("Index", "Users");
            }

            ViewBag.Student = studentId;
            return View(user);
        }

        [HttpPost]
        public ActionResult CancelReservation(int Location, int Time)
        {
            if (Session["Student"] != null)
            {
                string studentId = Session["Student"].ToString();
                var reservation = db.User.FirstOrDefault(r =>
                    r.Student == studentId);

                if (reservation != null)
                {
                    reservation.Location = Location;
                    reservation.Time = Time;
                    db.SaveChanges();
                }
            }
            return RedirectToAction("Home");
        }

        public JsonResult GetReservationStatus()
        {
            string studentId = Session["Student"]?.ToString();
            var reservation = db.User.FirstOrDefault(r => r.Student == studentId);

            return Json(new
            {
                Location = reservation?.Location ?? 0,
                Time = reservation?.Time ?? 0
            }, JsonRequestBehavior.AllowGet);
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

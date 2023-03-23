using LibraryWebServer.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace LibraryWebServer.Controllers
{
    public class HomeController : Controller
    {

        // WARNING:
        // This very simple web server is designed to be as tiny and simple as possible
        // This is NOT the way to save user data.
        // This will only allow one user of the web server at a time (aside from major security concerns).
        private static string user = "";
        private static int card = -1;

        private readonly ILogger<HomeController> _logger;


        /// <summary>
        /// Given a Patron name and CardNum, verify that they exist and match in the database.
        /// If the login is successful, sets the global variables "user" and "card"
        /// </summary>
        /// <param name="name">The Patron's name</param>
        /// <param name="cardnum">The Patron's card number</param>
        /// <returns>A JSON object with a single field: "success" with a boolean value:
        /// true if the login is accepted, false otherwise.
        /// </returns>
        [HttpPost]
        public IActionResult CheckLogin(string name, int cardnum)
        {
            bool loginSuccess = false;
            using (Team60LibraryContext db = new Team60LibraryContext())
            {
                var query =
                    from Patron in db.Patrons
                    where Patron.CardNum == cardnum && Patron.Name == name
                    select Patron;

                foreach (var v in query)
                {
                    Debug.WriteLine(v);
                }

                loginSuccess = query.Count() == 1;

            }

            if (loginSuccess)
            {
                card = cardnum;
                user = name;
            }
            return Json(new { success = loginSuccess });
        }


        /// <summary>
        /// Logs a user out. This is implemented for you.
        /// </summary>
        /// <returns>Success</returns>
        [HttpPost]
        public ActionResult LogOut()
        {
            user = "";
            card = -1;
            return Json(new { success = true });
        }

        /// <summary>
        /// Returns a JSON array representing all known books.
        /// Each book should contain the following fields:
        /// {"isbn" (string), "title" (string), "author" (string), "serial" (uint?), "name" (string)}
        /// Every object in the list should have isbn, title, and author.
        /// Books that are not in the Library's inventory (such as Dune) should have a null serial.
        /// The "name" field is the name of the Patron who currently has the book checked out (if any)
        /// Books that are not checked out should have an empty string "" for name.
        /// </summary>
        /// <returns>The JSON representation of the books</returns>
        [HttpPost]
        public ActionResult AllTitles()
        {

            using (Team60LibraryContext db = new Team60LibraryContext())
            {

                var query =
                    from Title in db.Titles
                    join InventoryItem in db.Inventory on Title.Isbn equals InventoryItem.Isbn into TitlesInventory


                    from JoinOneItem in TitlesInventory.DefaultIfEmpty()
                    join Checkout in db.CheckedOut on JoinOneItem.Serial equals Checkout.Serial into TitlesInventoryCheckout

                    from JoinTwoItem in TitlesInventoryCheckout.DefaultIfEmpty()
                    join Patron in db.Patrons on JoinTwoItem.CardNum equals Patron.CardNum into TitlesInventoryCheckoutPatrons

                    from FullRow in TitlesInventoryCheckoutPatrons.DefaultIfEmpty()

                    select new
                    {
                        Title = Title.Title,
                        Author = Title.Author,
                        ISBN = Title.Isbn,
                        Serial = JoinOneItem == null ? null : (uint?)JoinOneItem.Serial,
                        Name = FullRow == null ? "" : FullRow.Name
                    };

                Debug.WriteLine("Title  Author  ISBN   Serial   Name");
                foreach (var v in query)
                {
                    Debug.WriteLine(v);
                }

                return Json(query.ToArray());
            }

            //return Json(null);

        }

        /// <summary>
        /// Returns a JSON array representing all books checked out by the logged in user 
        /// The logged in user is tracked by the global variable "card".
        /// Every object in the array should contain the following fields:
        /// {"title" (string), "author" (string), "serial" (uint) (note this is not a nullable uint) }
        /// Every object in the list should have a valid (non-null) value for each field.
        /// </summary>
        /// <returns>The JSON representation of the books</returns>
        [HttpPost]
        public ActionResult ListMyBooks()
        {
            //select ISBN,Title,Author from CheckedOut natural join Inventory natural join Titles where CardNum =1;
            //select ISBN,Title,Author from CheckedOut natural join Inventory natural join Titles where CardNum = user;
            //TODO: implement
            using (Team60LibraryContext db = new Team60LibraryContext())
            {
                var query =
                    from Serial in db.CheckedOut
                    join InventoryItem in db.Inventory
                    on Serial.Serial equals InventoryItem.Serial
                    into SerialInventory

                    from SI in SerialInventory
                    join Title in db.Titles
                    on SI.Isbn equals Title.Isbn
                    into allData

                    from temp in allData
                    where Serial.CardNum == card

                    select new
                    {
                        ISBN = SI.Isbn,
                        Title = temp.Title,
                        Author = temp.Author,
                        Serial = SI.Serial
                    };

                foreach (var v in query)
                    Debug.WriteLine(v);
                return Json(query.ToArray());
            }
            //return Json(query.ToArray());
        }


        /// <summary>
        /// Updates the database to represent that
        /// the given book is checked out by the logged in user (global variable "card").
        /// In other words, insert a row into the CheckedOut table.
        /// You can assume that the book is not currently checked out by anyone.
        /// </summary>
        /// <param name="serial">The serial number of the book to check out</param>
        /// <returns>success</returns>
        [HttpPost]
        public ActionResult CheckOutBook(int serial)
        {
            // You may have to cast serial to a (uint)
            using (Team60LibraryContext db = new Team60LibraryContext())
            {
                //var query = from Patrons in db.Patrons
                //            where Patrons.CardNum == card
                //            select (Patrons.Name);

                CheckedOut newCheckout = new CheckedOut();
                newCheckout.CardNum = (uint)card;
                newCheckout.Serial = (uint)serial;
                db.CheckedOut.Add(newCheckout);
                db.SaveChanges();


            }


            return Json(new { success = true });
        }

        /// <summary>
        /// Returns a book currently checked out by the logged in user (global variable "card").
        /// In other words, removes a row from the CheckedOut table.
        /// You can assume the book is checked out by the user.
        /// </summary>
        /// <param name="serial">The serial number of the book to return</param>
        /// <returns>Success</returns>
        [HttpPost]
        public ActionResult ReturnBook(int serial)
        {
            using (Team60LibraryContext db = new Team60LibraryContext())
            {
                var query = from CheckedOutTable in db.CheckedOut
                            where (CheckedOutTable.CardNum == card) && (CheckedOutTable.Serial == serial)
                            select (CheckedOutTable);

                db.RemoveRange(query);
                db.SaveChanges();


            }

            return Json(new { success = true });
        }


        /*******************************************/
        /****** Do not modify below this line ******/
        /*******************************************/


        public IActionResult Index()
        {
            if (user == "" && card == -1)
                return View("Login");

            return View();
        }


        /// <summary>
        /// Return the Login page.
        /// </summary>
        /// <returns></returns>
        public IActionResult Login()
        {
            user = "";
            card = -1;

            ViewData["Message"] = "Please login.";

            return View();
        }

        /// <summary>
        /// Return the MyBooks page.
        /// </summary>
        /// <returns></returns>
        public IActionResult MyBooks()
        {
            if (user == "" && card == -1)
                return View("Login");

            return View();
        }

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
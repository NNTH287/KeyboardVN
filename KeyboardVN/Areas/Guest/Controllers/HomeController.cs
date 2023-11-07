﻿using KeyboardVN.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KeyboardVN.Areas.Guest.Controllers
{
    public class HomeController : Controller
    {
        private KeyboardVNContext context = new KeyboardVNContext();
        private IHttpContextAccessor httpContextAccessor = new HttpContextAccessor();
        [Area("Guest")]
        public IActionResult Index()
        {
            List<Category> categories = new List<Category>();
            List<Brand> brands = new List<Brand>();
            List<Product> products = new List<Product>();
            categories = context.Categories.ToList();
            brands = context.Brands.ToList();
            products = context.Products.OrderBy(p => p.UnitInStock).Take(3).ToList();
            ViewBag.Categories = categories;
            ViewBag.Brands = brands;
            ViewBag.Products = products;
            int productInCart = 0;
            if (httpContextAccessor.HttpContext.Session.GetInt32("userId") != null)
            {
                productInCart = context.CartItems.Where(ci => ci.CartId == context.Carts.FirstOrDefault(c => c.UserId == httpContextAccessor.HttpContext.Session.GetInt32("userId")).Id).Count();
            }
            ViewBag.productInCart = productInCart;
            return View();
        }
        [Area("Guest")]
        public IActionResult Details(int id)
        {
            var product = context.Products.Include(c=>c.Category).Include(c=>c.Brand).FirstOrDefault(product => product.Id == id);
            return View(product);
        }
        [Area("Guest")]
        public IActionResult ProductFilter(String? searchName,
                                    int? categoryId,
                                    String? price,
                                    int? brandId,
                                    String? sort,
                                    int? page)
        {
            List<Category> categories = new List<Category>();
            List<Brand> brands = new List<Brand>();

            categories = context.Categories.ToList();
            brands = context.Brands.ToList();

            if (searchName == null)
            {
                searchName = "";
            }
            if (categoryId == null)
            {
                categoryId = -1;
            }
            if (brandId == null)
            {
                brandId = -1;
            }
            if (sort == null)
            {
                sort = "";
            }
            if (page == null)
            {
                page = 1;
            }
            List<Product> listP = new List<Product>();
            string ConnectionString = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build().GetConnectionString("KeyboardVNContextConnection");
            SqlConnection conn = new SqlConnection(ConnectionString);
            double minPrice = 0;
            double maxPrice = 10000000000000;
            if (price != null && !price.Equals(""))
            {
                string[] tokens = price.Split('-');
                minPrice = double.Parse(tokens[0]);
                maxPrice = double.Parse(tokens[1]);
            }
            string sql = "";
            if (sort == "")
            {
                sql = @"select * from (select *, ROW_NUMBER() over (order by id) as r
               from product where categoryId in ("
                           + (categoryId == -1 ? "select id from Category" : "@CategoryId") + @")
               and brandid in ("
                           + (brandId == -1 ? "select id from Brand" : "@brandId") + @")
               and price between @MinPrice and @MaxPrice
               and [product].[name] like @SearchName) as x where r between 16*@Page-15 and 16*@Page";
            }
            else if (sort == "ascending")
            {
                sql = @"select * from (select *, ROW_NUMBER() over (order by id) as r
               from product where categoryId in ("
                           + (categoryId == -1 ? "select id from Category" : "@CategoryId") + @")
               and brandid in ("
                           + (brandId == -1 ? "select id from Brand" : "@brandId") + @")
               and price between @MinPrice and @MaxPrice
               and [product].[name] like @SearchName ) as x where r between 16*@Page-15 and 16*@Page order by price-discount";
            }
            else
            {
                sql = @"select * from (select *, ROW_NUMBER() over (order by id) as r
               from product where categoryId in ("
                             + (categoryId == -1 ? "select id from Category" : "@CategoryId") + @")
               and brandid in ("
                             + (brandId == -1 ? "select id from Brand" : "@brandId") + @")
               and price between @MinPrice and @MaxPrice
               and [product].[name] like @SearchName  ) as x where r between 16*@Page-15 and 16*@Page order by price-discount desc";
            }

            try
            {
                using (SqlCommand command = new SqlCommand(sql, conn))
                {
                    conn.Open();

                    command.Parameters.AddWithValue("@CategoryId", categoryId);
                    command.Parameters.AddWithValue("@brandId", brandId);
                    command.Parameters.AddWithValue("@MinPrice", minPrice);
                    command.Parameters.AddWithValue("@MaxPrice", maxPrice);
                    command.Parameters.AddWithValue("@SearchName", "%" + searchName + "%");
                    command.Parameters.AddWithValue("@Page", page);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int id = reader.GetInt32(0);
                            listP.Add(context.Products.FirstOrDefault(p => p.Id == id));
                        }
                    }
                    conn.Close();
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine(ex.Message);
            }
            ViewBag.categoryId = categoryId;
            ViewBag.brandId = brandId;
            ViewBag.price = price;
            ViewBag.sort = sort;
            ViewBag.searchName = searchName;
            ViewBag.page = page;
            ViewBag.Categories = categories;
            ViewBag.Brands = brands;
            ViewBag.Products = listP;
            int productInCart = 0;
            if (httpContextAccessor.HttpContext.Session.GetInt32("userId") != null)
            {
                int? userId = httpContextAccessor.HttpContext.Session.GetInt32("userId");
                ViewBag.UserId = userId;
                productInCart = context.CartItems.Where(ci => ci.CartId == context.Carts.FirstOrDefault(c => c.UserId == httpContextAccessor.HttpContext.Session.GetInt32("userId")).Id).Count();
            }
            ViewBag.productInCart = productInCart;
            ViewBag.numberOfProduct = context.Products.Count();
            return View();
        }
    }
}

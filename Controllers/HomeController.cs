using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Azure.Data.Tables;
using PART2.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Microsoft.VisualBasic;
using System.Security.Cryptography.X509Certificates;
using Azure.Storage.Files.Shares.Models;
using Azure.Storage.Files.Shares;

namespace PART2.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly TableServiceClient _tableServiceClient;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ShareServiceClient _shareServiceClient;

        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
            _tableServiceClient = new TableServiceClient(_configuration.GetConnectionString("AzureTableStorage"));
            _blobServiceClient = new BlobServiceClient(_configuration.GetConnectionString("AzureBlobStorage"));
            _shareServiceClient = new ShareServiceClient(_configuration.GetConnectionString("AzureFileShare"));
        }


        public IActionResult Index()
        {
            return View();
        }
        public IActionResult ContactUs()
        {
            return View();
        }

        public IActionResult AboutUs()
        {
            return View();
        }

        public async Task<IActionResult> Customers()
        {
            string connectionString = _configuration.GetConnectionString("AzureTableStorage");
            var tableClient = new TableClient(connectionString, "CustomerProfiles");
            await tableClient.CreateIfNotExistsAsync();

            List<CustomerProfiles> customers = new List<CustomerProfiles>();

            
            try
            {
                await foreach (var customer in tableClient.QueryAsync<CustomerProfiles>())
                {
                    customers.Add(customer);
                }
            }
            catch (RequestFailedException ex)
            {
                
                TempData["ErrorMessage"] = "Error retrieving customers: " + ex.Message;
            }

            return View(customers);
        }

        
        public IActionResult Create()
        {
            return View();
        }

        // POST: Home/Create
        [HttpPost]
        public async Task<IActionResult> Create(CustomerProfiles customer)
        {
            if (ModelState.IsValid)
            {
                try
                {
                  
                    customer.PartitionKey = "Customers";
                    customer.RowKey = Guid.NewGuid().ToString();

                    string connectionString = _configuration.GetConnectionString("AzureTableStorage");
                    var tableClient = new TableClient(connectionString, "CustomerProfiles");
                    await tableClient.CreateIfNotExistsAsync();
                    await tableClient.AddEntityAsync(customer);

                    TempData["SuccessMessage"] = "Customer information successfully stored!";
                    return RedirectToAction(nameof(Customers));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "An error occurred while adding the customer: " + ex.Message;
                }
            }
            return View(customer);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string rowKey)
        {
            if (string.IsNullOrEmpty(rowKey))
            {
                TempData["ErrorMessage"] = "Invalid customer ID.";
                return RedirectToAction(nameof(Customers));
            }

            try
            {
                string connectionString = _configuration.GetConnectionString("AzureTableStorage");
                var tableClient = new TableClient(connectionString, "CustomerProfiles");

                var customer = await tableClient.GetEntityAsync<CustomerProfiles>("Customers", rowKey);
                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Customer not found.";
                    return RedirectToAction(nameof(Customers));
                }

                return View(customer.Value);
            }
            catch (RequestFailedException)
            {
                TempData["ErrorMessage"] = "An error occurred while retrieving customer information.";
                return RedirectToAction(nameof(Customers));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Edit(CustomerProfiles updatedCustomer)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string connectionString = _configuration.GetConnectionString("AzureTableStorage");
                    var tableClient = new TableClient(connectionString, "CustomerProfiles");

                    await tableClient.UpsertEntityAsync(updatedCustomer, TableUpdateMode.Replace);
                    TempData["SuccessMessage"] = "Customer information successfully updated!";
                    return RedirectToAction(nameof(Customers));
                }
                catch (RequestFailedException)
                {
                    TempData["ErrorMessage"] = "An error occurred while updating customer information.";
                    return View(updatedCustomer);
                }
            }
            return View(updatedCustomer);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string rowKey)
        {
            if (string.IsNullOrEmpty(rowKey))
            {
                TempData["ErrorMessage"] = "Invalid customer ID.";
                return RedirectToAction(nameof(Customers));
            }

            try
            {
                string connectionString = _configuration.GetConnectionString("AzureTableStorage");
                var tableClient = new TableClient(connectionString, "CustomerProfiles");

                var customer = await tableClient.GetEntityAsync<CustomerProfiles>("Customers", rowKey);
                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Customer not found.";
                    return RedirectToAction(nameof(Customers));
                }

                return View(customer.Value);
            }
            catch (RequestFailedException)
            {
                TempData["ErrorMessage"] = "An error occurred while retrieving customer information.";
                return RedirectToAction(nameof(Customers));
            }
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(string rowKey)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("AzureTableStorage");
                var tableClient = new TableClient(connectionString, "CustomerProfiles");

                await tableClient.DeleteEntityAsync("Customers", rowKey);
                TempData["SuccessMessage"] = "Customer information successfully deleted!";
                return RedirectToAction(nameof(Customers));
            }
            catch (RequestFailedException)
            {
                TempData["ErrorMessage"] = "An error occurred while deleting customer information.";
                return RedirectToAction(nameof(Customers));
            }
        }


        public IActionResult Products()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> UploadBlob(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Content("File not selected");

            var containerClient =
_blobServiceClient.GetBlobContainerClient("products");
            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient(file.FileName);
            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, true);
            }

            return RedirectToAction("Products");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadBlob(string blobName)
        {
            var containerClient =
_blobServiceClient.GetBlobContainerClient("products");

            var blobClient = containerClient.GetBlobClient(blobName);
            var download = await blobClient.DownloadAsync();

            return File(download.Value.Content, "application/octet-stream", blobName);
        }



        public async Task<IActionResult> ListFiles()
        {
            try
            {
                var shareClient = _shareServiceClient.GetShareClient("logsandpolicies");
                var directoryClient = shareClient.GetRootDirectoryClient();

                // Ensure the share and directory exist
                await shareClient.CreateIfNotExistsAsync();

                var fileItems = directoryClient.GetFilesAndDirectoriesAsync();

                var fileList = new List<string>();

                await foreach (var fileItem in fileItems)
                {
                    if (!fileItem.IsDirectory) // Only add files, skip directories
                    {
                        fileList.Add(fileItem.Name);
                    }
                }

                // If no files were found, display a message in the view
                if (fileList.Count == 0)
                {
                    ViewBag.Message = "No files found in Azure File Share.";
                }

                return View(fileList); // Passing the list to the view
            }
            catch (Exception ex)
            {
                // Log the error and show an error message in the view
                ViewBag.ErrorMessage = $"Error retrieving files: {ex.Message}";
                return View(new List<string>()); // Return an empty list in case of an error
            }
        }
        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Content("File not selected");

            try
            {
                var shareClient = _shareServiceClient.GetShareClient("logsandpolicies");
                await shareClient.CreateIfNotExistsAsync();

                var directoryClient = shareClient.GetRootDirectoryClient();
                var fileClient = directoryClient.GetFileClient(file.FileName);

                using (var stream = file.OpenReadStream())
                {
                    await fileClient.CreateAsync(file.Length);
                    await fileClient.UploadAsync(stream);
                }

                ViewBag.Message = "File uploaded successfully!";
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error uploading file: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                ViewBag.ErrorMessage = "File name cannot be empty.";
                return RedirectToAction("Index");
            }

            try
            {
                var shareClient = _shareServiceClient.GetShareClient("logsandpolicies");
                var directoryClient = shareClient.GetRootDirectoryClient();
                var fileClient = directoryClient.GetFileClient(fileName);

                var downloadResponse = await fileClient.DownloadAsync();

                return File(downloadResponse.Value.Content, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error downloading file: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
    }
}







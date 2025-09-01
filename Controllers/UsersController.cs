using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient; // Asegúrate de instalar Microsoft.Data.SqlClient
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
[Authorize]
public class UsersController : Controller
{
    private readonly string _connectionString;

    public UsersController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    // Acción para obtener los usuarios con sus roles
    public async Task<IActionResult> Index()
    {
        var usersWithRoles = new List<dynamic>();

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = new SqlCommand(@"
                SELECT u.UserName, u.FirstName, u.LastName, r.Name AS Rol
                FROM AspNetUsers AS u
                JOIN AspNetUserRoles AS ur ON u.Id = ur.UserId
                JOIN AspNetRoles AS r ON ur.RoleId = r.Id", connection))
            {
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var user = new
                        {
                            UserName = reader["UserName"].ToString(),
                            FirstName = reader["FirstName"].ToString(),
                            LastName = reader["LastName"].ToString(),
                            Rol = reader["Rol"].ToString()
                        };
                        usersWithRoles.Add(user);
                    }
                }
            }
        }

        return View(usersWithRoles);
    }
}

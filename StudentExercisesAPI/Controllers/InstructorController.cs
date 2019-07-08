using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using StudentExercisesAPI.Models;

namespace StudentExercisesAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class InstructorController : ControllerBase
    {
        private readonly IConfiguration _config;

        public InstructorController(IConfiguration config)
        {
            _config = config;
        }

        public SqlConnection Connection
        {
            get
            {
                return new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            }
        }

        private bool InstructorExists(int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT Id FROM Instructor WHERE Id = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));

                    SqlDataReader reader = cmd.ExecuteReader();
                    return reader.Read();
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetInstructors(int? cohort, string q, string orderBy)
        {
            string sql = @"SELECT 
                            i.Id, i.FirstName, i.LastName, i.SlackHandle, i.Specialty
                            c.Id CohortId, c.Name CohortName
                        FROM Instructor i
                        JOIN Cohort c ON i.CohortId = c.Id
                        WHERE 2=2
                        ";

            if (cohort != null)
            {
                sql = $"{sql} AND s.CohortId = @cohortId";
            }

            if (q != null)
            {
                sql = $@"{sql} AND (
                    s.LastName LIKE @q
                    OR s.FirstName LIKE @q
                    OR s.SlackHandle LIKE @q
                    )
                    ";

            }

            if (orderBy != null)
            {
                sql = $"{sql} ORDER BY {orderBy}";
            }

            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    if (cohort != null)
                    {
                        cmd.Parameters.Add(new SqlParameter("@cohortId", cohort));
                    }
                    if (q != null)
                    {
                        cmd.Parameters.Add(new SqlParameter("@q", $"%{q}%"));

                    }

                    SqlDataReader reader = await cmd.ExecuteReaderAsync();

                    List<Instructor> instructorList = new List<Instructor>();

                    while (reader.Read())
                    {
                        Instructor Instructor = new Instructor
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            CohortId = reader.GetInt32(reader.GetOrdinal("CohortId")),
                            FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                            LastName = reader.GetString(reader.GetOrdinal("LastName")),
                            Specialty = reader.GetString(reader.GetOrdinal("Specialty")),
                                SlackHandle = reader.GetString(reader.GetOrdinal("SlackHandle")),
                                Cohort = new Cohort
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("CohortId")),
                                    Name = reader.GetString(reader.GetOrdinal("CohortName"))
                                }
                            };
                        instructorList.Add(Instructor);
                        }
                    reader.Close();

                    return Ok(instructorList);
                }
            }
        }

        [HttpGet("{id}", Name = "GetInstructor")]
        public async Task<IActionResult> Get([FromRoute] int id)
        {
            if (!InstructorExists(id))
            {
                return new StatusCodeResult(StatusCodes.Status404NotFound);
            }
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT 
                                            i.Id, i.FirstName, i.LastName, i.SlackHandle, i.Specialty
                                            c.Id CohortId, c.Name CohortName,
                                        FROM Instructor i
                                        JOIN Cohort c ON s.CohortId = c.Id
                                        WHERE i.Id = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));
                    SqlDataReader reader = await cmd.ExecuteReaderAsync();

                    Instructor instructor = null;

                    while (reader.Read())
                    {
                        if (instructor == null)
                        {
                            instructor = new Instructor
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                CohortId = reader.GetInt32(reader.GetOrdinal("CohortId")),
                                FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                LastName = reader.GetString(reader.GetOrdinal("LastName")),
                                Specialty = reader.GetString(reader.GetOrdinal("Specialty")),
                                SlackHandle = reader.GetString(reader.GetOrdinal("SlackHandle")),
                                Cohort = new Cohort
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("CohortId")),
                                    Name = reader.GetString(reader.GetOrdinal("CohortName"))

                                }
                            };
                        }
                    }
                    reader.Close();

                    return Ok(instructor);
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Instructor instructor)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO Instructor (FirstName, LastName, SlackHandle, CohortId, Specialty)
                                        OUTPUT INSERTED.Id
                                        VALUES (@firstName, @lastName, @handle, @cId, @specialty)";
                    cmd.Parameters.Add(new SqlParameter("@firstName", instructor.FirstName));
                    cmd.Parameters.Add(new SqlParameter("@lastName", instructor.LastName));
                    cmd.Parameters.Add(new SqlParameter("@handle", instructor.SlackHandle));
                    cmd.Parameters.Add(new SqlParameter("@cId", instructor.CohortId));
                    cmd.Parameters.Add(new SqlParameter("@specialty", instructor.Specialty));

                    int newId = (int)await cmd.ExecuteScalarAsync();
                    instructor.Id = newId;
                    return CreatedAtRoute("GetInstructor", new { id = newId }, instructor);
                }
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put([FromRoute] int id, [FromBody] Instructor instructor)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"UPDATE Instructor
                                            SET FirstName = @firstName,
                                                LastName = @lastName,
                                                SlackHandle = @handle,
                                                Specialty = @specialty,
                                                CohortId = @cId
                                            WHERE Id = @id";
                        cmd.Parameters.Add(new SqlParameter("@firstName", instructor.FirstName));
                        cmd.Parameters.Add(new SqlParameter("@lastName", instructor.LastName));
                        cmd.Parameters.Add(new SqlParameter("@handle", instructor.SlackHandle));
                        cmd.Parameters.Add(new SqlParameter("@cId", instructor.CohortId));
                        cmd.Parameters.Add(new SqlParameter("@specialty", instructor.Specialty));
                        cmd.Parameters.Add(new SqlParameter("@id", id));

                        //gets number of rows affected.
                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                        {
                            //if no rows affected
                            return new StatusCodeResult(StatusCodes.Status204NoContent);
                        }
                        throw new Exception("No rows affected");
                    }
                }
            }
            catch (Exception)
            {
                if (!InstructorExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"DELETE FROM Instructor WHERE Id = @id";
                        cmd.Parameters.Add(new SqlParameter("@id", id));

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                        {
                            return new StatusCodeResult(StatusCodes.Status204NoContent);
                        }
                        throw new Exception("No rows affected");
                    }
                }
            }
            catch (Exception)
            {
                if (!InstructorExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
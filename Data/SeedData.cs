using System;
using BTL_QuanLyLopHocTrucTuyen.Helpers;
using BTL_QuanLyLopHocTrucTuyen.Models;
using BTL_QuanLyLopHocTrucTuyen.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BTL_QuanLyLopHocTrucTuyen.Data;

/// <summary>
/// Khởi tạo dữ liệu ban đầu cho hệ thống
/// </summary>
public static class SeedData
{
    public static void Initialize(IServiceProvider serviceProvider) 
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            // Ensure database is created
            context.Database.EnsureCreated();

            // Seed in order: Roles -> Users -> Tenants -> Courses
            SeedRoles(context, logger);
            SeedAdminUser(context, logger);
            SeedSampleData(context, logger);

            logger.LogInformation("✅ Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Error occurred while seeding database");
            throw;
        }
    }

    /// <summary>
    /// Seed default roles
    /// </summary>
    private static void SeedRoles(ApplicationDbContext context, ILogger logger)
    {
        if (context.Roles.Any())
        {
            logger.LogInformation("Roles already exist, skipping...");
            return;
        }

        var roles = new[]
        {
            new Role
            {
                Name = "Default",
                Description = "Default role with no permissions",
                Permissions = UserPermission.None
            },
            new Role
            {
                Name = "Administrator",
                Description = "System administrator with full permissions",
                Permissions = UserPermission.Administrator,
                TenantId = null
            },
            new Role
            {
                Name = "Manager",
                Description = "Tenant manager role - can manage tenant settings and users",
                Permissions = UserPermission.Manager
            },
            new Role
            {
                Name = "Instructor",
                Description = "Teacher role - can manage courses and assignments",
                Permissions = UserPermission.Instructor
            },
            new Role
            {
                Name = "Student",
                Description = "Student role - can view courses and submit assignments",
                Permissions = UserPermission.Student
            }
        };

        context.Roles.AddRange(roles);
        context.SaveChanges();
        logger.LogInformation($"✅ Seeded {roles.Length} roles");
    }

    /// <summary>
    /// Seed admin user
    /// </summary>
    private static void SeedAdminUser(ApplicationDbContext context, ILogger logger)
    {
        if (context.Users.Any())
        {
            logger.LogInformation("Users already exist, skipping...");
            return;
        }

        var adminRole = context.Roles.FirstOrDefault(r => r.Name == "Administrator");
        if (adminRole == null)
        {
            logger.LogWarning("Administrator role not found, cannot create admin user");
            return;
        }

        var adminUser = new User
        {
            FullName = "System Administrator",
            Email = "admin@system.com",
            PasswordHash = SecurityHelper.HashPassword("Admin@123"),
            RoleId = adminRole.Id
        };

        context.Users.Add(adminUser);
        context.SaveChanges();
        logger.LogInformation("✅ Seeded admin user: {Email}", adminUser.Email);
    }

    /// <summary>
    /// Seed sample data for testing (optional)
    /// </summary>
    private static void SeedSampleData(ApplicationDbContext context, ILogger logger)
    {
        // Check if already seeded
        if (context.Tenants.Any())
        {
            logger.LogInformation("Sample data already exists, skipping...");
            return;
        }

        try
        {
            // Get roles
            var managerRole = context.Roles.First(r => r.Name == "Manager");
            var instructorRole = context.Roles.First(r => r.Name == "Instructor");
            var studentRole = context.Roles.First(r => r.Name == "Student");

            // Create sample manager
            var manager = new User
            {
                FullName = "Le Van B",
                Email = "manager@example.com",
                PasswordHash = SecurityHelper.HashPassword("Manager@123"),
                RoleId = managerRole.Id
            };
            context.Users.Add(manager);
            context.SaveChanges();

            // Create sample tenant
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = "Sample School",
                Plan = PlanType.Premium,
                OwnerId = manager.Id,
                CreatedAt = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddYears(1)
            };
            context.Tenants.Add(tenant);
            context.SaveChanges();

            // Update manager's tenant
            manager.TenantId = tenant.Id;
            context.SaveChanges();

            // Create sample instructor
            var instructor = new User
            {
                FullName = "Tran Thi B",
                Email = "instructor@example.com",
                PasswordHash = SecurityHelper.HashPassword("Instructor@123"),
                RoleId = instructorRole.Id,
                TenantId = tenant.Id
            };

            // Create sample students
            var student1 = new User
            {
                FullName = "Le Van C",
                Email = "student1@example.com",
                PasswordHash = SecurityHelper.HashPassword("Student@123"),
                RoleId = studentRole.Id,
                TenantId = tenant.Id
            };

            var student2 = new User
            {
                FullName = "Pham Thi D",
                Email = "student2@example.com",
                PasswordHash = SecurityHelper.HashPassword("Student@123"),
                RoleId = studentRole.Id,
                TenantId = tenant.Id
            };

            context.Users.AddRange(instructor, student1, student2);
            context.SaveChanges();

            // Create sample course
            var course = new Course
            {
                Name = "Lập trình Web với ASP.NET Core",
                Description = "Khóa học về phát triển web với ASP.NET Core MVC",
                BeginTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddMonths(3),
                Status = ScheduleStatus.Active,
                TenantId = tenant.Id,
                InstructorId = instructor.Id,
                CreatedAt = DateTime.UtcNow
            };
            context.Courses.Add(course);
            context.SaveChanges();

            // Create sample lesson
            var lesson = new Lesson
            {
                Title = "Bài 1: Giới thiệu ASP.NET Core",
                Content = "Tìm hiểu về kiến trúc MVC và cách tạo project",
                CourseId = course.Id,
                BeginTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddHours(2),
                Status = ScheduleStatus.Active
            };
            context.Lessons.Add(lesson);
            context.SaveChanges();

            // Create sample assignment
            var assignment = new Assignment
            {
                Title = "Bài tập 1: Tạo ứng dụng Hello World",
                Description = "Tạo một ứng dụng ASP.NET Core MVC đơn giản hiển thị Hello World",
                LessonId = lesson.Id,
                //DueDate = DateTime.UtcNow.AddDays(7),
                MaxScore = 100,
                Type = "Homework",
                bDisabled = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Assignments.Add(assignment);
            context.SaveChanges();

            // Enroll students
            var enrollment1 = new Enrollment
            {
                UserId = student1.Id,
                CourseId = course.Id,
                EnrolledAt = DateTime.UtcNow,
                Status = EnrollmentStatus.Enrolled,
                Progress = 0
            };

            var enrollment2 = new Enrollment
            {
                UserId = student2.Id,
                CourseId = course.Id,
                EnrolledAt = DateTime.UtcNow,
                Status = EnrollmentStatus.Enrolled,
                Progress = 0
            };

            context.Enrollments.AddRange(enrollment1, enrollment2);
            context.SaveChanges();

            logger.LogInformation("✅ Seeded sample data: 1 tenant, 1 course, 2 students");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to seed sample data (this is optional)");
        }
    }
}
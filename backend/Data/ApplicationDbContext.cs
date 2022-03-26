using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace backend.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Group> Group { get; set; }
        public DbSet<GroupChat> GroupChat { get; set; }
        public DbSet<GroupChatSeen> GroupChatSeen { get; set; }
        public DbSet<GroupUser> GroupUser { get; set; }
        public DbSet<Notification> Notification { get; set; }
        public DbSet<PrivateChat> PrivateChat { get; set; }
        public DbSet<VideoCall> VideoCall { get; set; }

        // For changing table name in database, and also set composite key
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Notification, ApplicationUser ----------------------------------------------------
            modelBuilder.Entity<Notification>()
                .HasOne(x => x.User)
                .WithMany(y => y.Notifications)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // PrivateChat, ApplicationUser -----------------------------------------------------
            modelBuilder.Entity<PrivateChat>()
                .HasOne(x => x.Sender)
                .WithMany(y => y.SentPrivateChats)
                .HasForeignKey(x => x.SenderId)
                .OnDelete(DeleteBehavior.ClientCascade);

            modelBuilder.Entity<PrivateChat>()
                .HasOne(x => x.Receiver)
                .WithMany(y => y.ReceivedPrivateChats)
                .HasForeignKey(x => x.ReceiverId)
                .OnDelete(DeleteBehavior.ClientCascade);

            // Group, GroupUser, ApplicationUser ------------------------------------------------
            modelBuilder.Entity<GroupUser>()
                .HasKey(x => new { x.GroupId, x.UserId });

            modelBuilder.Entity<GroupUser>()
                .HasOne(x => x.Group)
                .WithMany(y => y.GroupUsers)
                .HasForeignKey(x => x.GroupId)
                .OnDelete(DeleteBehavior.ClientCascade);

            modelBuilder.Entity<GroupUser>()
                .HasOne(x => x.User)
                .WithMany(y => y.GroupUsers)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.ClientCascade);

            // GroupChat, ApplicationUser ------------------------------------------------------
            modelBuilder.Entity<GroupChat>()
                .HasOne(x => x.Sender)
                .WithMany(y => y.GroupChats)
                .HasForeignKey(x => x.SenderId)
                .OnDelete(DeleteBehavior.ClientCascade);

            modelBuilder.Entity<GroupChat>()
                .HasOne(x => x.Group)
                .WithMany(y => y.GroupChats)
                .HasForeignKey(x => x.GroupId)
                .OnDelete(DeleteBehavior.ClientCascade);

            // GroupChat, GroupChatSeen, ApplicationUser ---------------------------------------
            modelBuilder.Entity<GroupChatSeen>()
                .HasKey(x => new { x.GroupChatId, x.UserId });

            modelBuilder.Entity<GroupChatSeen>()
                .HasOne(x => x.GroupChat)
                .WithMany(y => y.GroupChatSeens)
                .HasForeignKey(x => x.GroupChatId)
                .OnDelete(DeleteBehavior.ClientCascade);

            modelBuilder.Entity<GroupChatSeen>()
                .HasOne(x => x.User)
                .WithMany(y => y.GroupChatSeens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.ClientCascade);

            //modelBuilder.Entity<Course>().ToTable("Course");
            //modelBuilder.Entity<Enrollment>().ToTable("Enrollment");
            //modelBuilder.Entity<Student>().ToTable("Student");
            //modelBuilder.Entity<Department>().ToTable("Department");
            //modelBuilder.Entity<Instructor>().ToTable("Instructor");
            //modelBuilder.Entity<OfficeAssignment>().ToTable("OfficeAssignment");
            //modelBuilder.Entity<CourseAssignment>().ToTable("CourseAssignment");

            //modelBuilder.Entity<CourseAssignment>()
            //    .HasKey(c => new { c.CourseID, c.InstructorID });
            base.OnModelCreating(modelBuilder);
        }
    }
}

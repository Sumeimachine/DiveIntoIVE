using Microsoft.EntityFrameworkCore;
using DiveIntoIVE.Models;

namespace DiveIntoIVE.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<UserRefreshToken> UserRefreshTokens { get; set; }
        public DbSet<MemberProfile> MemberProfiles { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<QuizQuestion> QuizQuestions { get; set; }
        public DbSet<AnswerOption> AnswerOptions { get; set; }
        public DbSet<QuizAttempt> QuizAttempts { get; set; }
        public DbSet<QuizAttemptAnswer> QuizAttemptAnswers { get; set; }
        public DbSet<EventReward> EventRewards { get; set; }
        public DbSet<UserEventRewardClaim> UserEventRewardClaims { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<MemberProfile>()
                .HasIndex(profile => profile.MemberKey)
                .IsUnique();

            modelBuilder.Entity<Quiz>()
                .HasIndex(quiz => quiz.Slug)
                .IsUnique();

            modelBuilder.Entity<QuizQuestion>()
                .HasOne(question => question.Quiz)
                .WithMany(quiz => quiz.Questions)
                .HasForeignKey(question => question.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AnswerOption>()
                .HasOne(option => option.Question)
                .WithMany(question => question.Options)
                .HasForeignKey(option => option.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizAttempt>()
                .HasOne(attempt => attempt.Quiz)
                .WithMany(quiz => quiz.Attempts)
                .HasForeignKey(attempt => attempt.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizAttempt>()
                .HasOne(attempt => attempt.User)
                .WithMany()
                .HasForeignKey(attempt => attempt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizAttemptAnswer>()
                .HasOne(answer => answer.Attempt)
                .WithMany(attempt => attempt.Answers)
                .HasForeignKey(answer => answer.QuizAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizAttemptAnswer>()
                .HasOne(answer => answer.Question)
                .WithMany()
                .HasForeignKey(answer => answer.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<QuizAttemptAnswer>()
                .HasOne(answer => answer.AnswerOption)
                .WithMany()
                .HasForeignKey(answer => answer.AnswerOptionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserEventRewardClaim>()
                .HasOne(claim => claim.User)
                .WithMany(user => user.EventRewardClaims)
                .HasForeignKey(claim => claim.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserEventRewardClaim>()
                .HasOne(claim => claim.EventReward)
                .WithMany(eventReward => eventReward.Claims)
                .HasForeignKey(claim => claim.EventRewardId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserEventRewardClaim>()
                .HasIndex(claim => new { claim.UserId, claim.EventRewardId })
                .IsUnique();
        }
    }
}

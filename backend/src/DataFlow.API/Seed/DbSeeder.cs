using DataFlow.Business.Common;
using DataFlow.Business.Dtos.Rules;
using DataFlow.DataAccess.Context;
using DataFlow.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataFlow.API.Seed;

/// <summary>
/// İlk çalıştırmada demo kullanıcıları ve hazır kural şablonlarını yükler.
/// Sunum sırasında sıfırdan veri girmeye gerek kalmaz.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, IConfiguration config)
    {
        var password = config.GetValue("Seed:DemoPassword", "Demo1234!")!;

        if (!await db.Users.AnyAsync())
        {
            db.Users.AddRange(
                new User
                {
                    Username = "admin",
                    Email = "admin@dataflow.local",
                    FullName = "Sistem Yöneticisi",
                    Role = "Admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
                },
                new User
                {
                    Username = "analist",
                    Email = "analist@dataflow.local",
                    FullName = "Veri Analisti",
                    Role = "Analyst",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
                });

            await db.SaveChangesAsync();
        }

        if (!await db.RulePresets.AnyAsync(p => p.IsSystemPreset))
        {
            db.RulePresets.AddRange(
                new RulePreset
                {
                    Name = "Satış — Yaş Segmentasyonu",
                    Description = "Yaşa göre X/Y/Z segmenti atar, boş doğum yerini 'Belirsiz' yapar.",
                    Category = "satis",
                    IsSystemPreset = true,
                    RulesJson = JsonDefaults.Serialize(SalesSegmentationRules())
                },
                new RulePreset
                {
                    Name = "Genel Temizlik",
                    Description = "Boşlukları kırpar, tekrar eden satırları siler, boş kayıtları eler.",
                    Category = "genel",
                    IsSystemPreset = true,
                    RulesJson = JsonDefaults.Serialize(GeneralCleanupRules())
                },
                new RulePreset
                {
                    Name = "Çalışan — Maaş Düzenleme",
                    Description = "Maaşı sayıya çevirir, eksik departmanı doldurur, düşük maaşlara zam uygular.",
                    Category = "calisan",
                    IsSystemPreset = true,
                    RulesJson = JsonDefaults.Serialize(EmployeeRules())
                });

            await db.SaveChangesAsync();
        }
    }

    /// <summary>Beyaz tahtadaki senaryonun birebir karşılığı.</summary>
    private static List<RuleDto> SalesSegmentationRules() => new()
    {
        new RuleDto
        {
            Order = 1,
            Name = "Doğum yeri boşsa 'Belirsiz' yaz",
            Condition = Group(Cond("DogumYeri", ConditionOperators.IsNull)),
            Action = new RuleActionDto
            {
                Type = ActionTypes.SetValue,
                TargetColumn = "DogumYeri",
                Value = "Belirsiz"
            }
        },
        new RuleDto
        {
            Order = 2,
            Name = "Yaş > 50 ise segment X",
            Condition = Group(Cond("Yas", ConditionOperators.GreaterThan, "50")),
            Action = new RuleActionDto
            {
                Type = ActionTypes.SetValue,
                TargetColumn = "Segment",
                Value = "X"
            }
        },
        new RuleDto
        {
            Order = 3,
            Name = "Yaş < 5 ise segment Y",
            Condition = Group(Cond("Yas", ConditionOperators.LessThan, "5")),
            Action = new RuleActionDto
            {
                Type = ActionTypes.SetValue,
                TargetColumn = "Segment",
                Value = "Y"
            }
        },
        new RuleDto
        {
            Order = 4,
            Name = "Kalanlara segment Z",
            Condition = Group(Cond("Segment", ConditionOperators.IsNull)),
            Action = new RuleActionDto
            {
                Type = ActionTypes.SetValue,
                TargetColumn = "Segment",
                Value = "Z"
            }
        }
    };

    private static List<RuleDto> GeneralCleanupRules() => new()
    {
        new RuleDto
        {
            Order = 1,
            Name = "Tekrar eden satırları sil",
            Action = new RuleActionDto { Type = ActionTypes.Deduplicate }
        },
        new RuleDto
        {
            Order = 2,
            Name = "Ad alanını temizle ve düzgün yaz",
            Action = new RuleActionDto { Type = ActionTypes.Trim, TargetColumn = "Ad" }
        },
        new RuleDto
        {
            Order = 3,
            Name = "Ad alanını İlk Harf Büyük yap",
            Action = new RuleActionDto { Type = ActionTypes.ToTitleCase, TargetColumn = "Ad" }
        }
    };

    private static List<RuleDto> EmployeeRules() => new()
    {
        new RuleDto
        {
            Order = 1,
            Name = "Maaşı sayıya çevir",
            Action = new RuleActionDto { Type = ActionTypes.CastNumber, TargetColumn = "Maas" }
        },
        new RuleDto
        {
            Order = 2,
            Name = "Departmanı boş olanları 'Atanmadı' yap",
            Condition = Group(Cond("Departman", ConditionOperators.IsNull)),
            Action = new RuleActionDto
            {
                Type = ActionTypes.SetValue,
                TargetColumn = "Departman",
                Value = "Atanmadı"
            }
        },
        new RuleDto
        {
            Order = 3,
            Name = "Maaşı 20.000 altındakilere %10 zam",
            Condition = Group(Cond("Maas", ConditionOperators.LessThan, "20000")),
            Action = new RuleActionDto
            {
                Type = ActionTypes.Multiply,
                TargetColumn = "Maas",
                Value = "1.1"
            }
        },
        new RuleDto
        {
            Order = 4,
            Name = "Maaşı yuvarla",
            Action = new RuleActionDto
            {
                Type = ActionTypes.Round,
                TargetColumn = "Maas",
                Value = "2"
            }
        }
    };

    private static ConditionGroupDto Group(params ConditionDto[] conditions)
        => new() { Logic = "AND", Conditions = conditions.ToList() };

    private static ConditionDto Cond(string column, string op, string? value = null)
        => new() { Column = column, Operator = op, Value = value };
}

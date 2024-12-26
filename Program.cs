using Microsoft.EntityFrameworkCore;
using TodoApi;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// הוספת DbContext (שימוש ב-MySQL לדוגמה, תוכל לשנות לפי הצורך)
builder.Services.AddDbContext<ToDoDbContext>(options =>
    options.UseMySql("server=localhost;database=myDB;user=root;password=12345", 
    Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.40-mysql")));
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", builder =>
    {
        builder.WithOrigins("http://localhost:3000") // כתובת ה-React
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// הוספת Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();
app.UseCors("AllowReactApp");

// הצגת Swagger רק אם אנחנו בסביבת פיתוח
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ToDo API v1");
    });
}

// הגדרת API
app.MapGet("/", () => "Hello World!");

// שליפת פריטים
app.MapGet("/items", async (ToDoDbContext context) =>
{
    var items = await context.Items.ToListAsync();
    return Results.Ok(items);
});

// הוספת פריט חדש
app.MapPost("/items", async (ToDoDbContext context, [FromBody] string name) =>
{
    var item = await context.Items
        .OrderByDescending(i => i.Id)
        .FirstOrDefaultAsync();

    var newItem = new Item
    {
        Name = name,
        IsComplete = false,
        Id = item?.Id + 1 ?? 1
    };

    await context.Items.AddAsync(newItem);
    await context.SaveChangesAsync();

    return Results.Created($"/items/{newItem.Id}", newItem);
});


app.MapPut("/items/{id}", async (ToDoDbContext context, int id, [FromBody] JsonElement body) =>
{
    if (body.TryGetProperty("isComplete", out var isCompleteProperty) && isCompleteProperty.ValueKind == JsonValueKind.True || isCompleteProperty.ValueKind == JsonValueKind.False)
    {
        var item = await context.Items.FindAsync(id);
        if (item == null)
        {
            return Results.NotFound();
        }

        item.IsComplete = isCompleteProperty.GetBoolean();
        await context.SaveChangesAsync();

        return Results.Ok(item);
    }
    return Results.BadRequest("Invalid request body.");
});



app.MapDelete("/items/{id}", async (ToDoDbContext context, int id) =>
{
    var item = await context.Items.FindAsync(id);

    if (item == null)
    {
        return Results.NotFound("Item not found");
    }

    context.Items.Remove(item);
    await context.SaveChangesAsync();

    return Results.Ok("Deleted successfully");
});

app.Run();

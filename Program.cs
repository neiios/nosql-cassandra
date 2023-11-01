using System.Text.Json;
using Cassandra;
using CassandraChat.Messages;
using CassandraChat.Rooms;
using CassandraChat.Users;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var cluster = Cluster.Builder()
    .AddContactPoint("localhost")
    .Build();

var session = cluster.Connect("nosql_chat");

app.MapGroup("/api/v1/users")
    .MapUsersApi(session);

app.MapGroup("/api/v1/rooms")
    .MapRoomsApi(session)
    .MapMessagesApi(session);

app.Run();

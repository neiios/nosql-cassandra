using Cassandra;
using CassandraChat.Users;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.Run();

﻿// <auto-generated />
using ManoelLoja.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace ManoelLoja.Migrations
{
    [DbContext(typeof(LojaManoelDbContext))]
    [Migration("20250528114806_InitialCreate")]
    partial class InitialCreate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("ManoelLoja.Models.Caixa", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<decimal>("Altura")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("Comprimento")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("Largura")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("Nome")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasAnnotation("Relational:JsonPropertyName", "caixa_id");

                    b.HasKey("Id");

                    b.ToTable("Caixas");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            Altura = 30m,
                            Comprimento = 80m,
                            Largura = 40m,
                            Nome = "Caixa 1"
                        },
                        new
                        {
                            Id = 2,
                            Altura = 80m,
                            Comprimento = 40m,
                            Largura = 50m,
                            Nome = "Caixa 2"
                        },
                        new
                        {
                            Id = 3,
                            Altura = 50m,
                            Comprimento = 60m,
                            Largura = 80m,
                            Nome = "Caixa 3"
                        });
                });
#pragma warning restore 612, 618
        }
    }
}

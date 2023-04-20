USE [master]
GO
/****** Object:  Database [leontest1]    Script Date: 4/20/2023 2:17:03 PM ******/
CREATE DATABASE [leontest1]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'leontest1', FILENAME = N'D:\Data\SQLServer\Data\leontest1.mdf' , SIZE = 3264KB , MAXSIZE = UNLIMITED, FILEGROWTH = 1024KB )
 LOG ON 
( NAME = N'leontest1_log', FILENAME = N'D:\Data\SQLServer\Data\leontest1_log.ldf' , SIZE = 12608KB , MAXSIZE = 2048GB , FILEGROWTH = 10%)
GO
ALTER DATABASE [leontest1] SET COMPATIBILITY_LEVEL = 120
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [leontest1].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [leontest1] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [leontest1] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [leontest1] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [leontest1] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [leontest1] SET ARITHABORT OFF 
GO
ALTER DATABASE [leontest1] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [leontest1] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [leontest1] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [leontest1] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [leontest1] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [leontest1] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [leontest1] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [leontest1] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [leontest1] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [leontest1] SET  ENABLE_BROKER 
GO
ALTER DATABASE [leontest1] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [leontest1] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [leontest1] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [leontest1] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [leontest1] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [leontest1] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [leontest1] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [leontest1] SET RECOVERY SIMPLE 
GO
ALTER DATABASE [leontest1] SET  MULTI_USER 
GO
ALTER DATABASE [leontest1] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [leontest1] SET DB_CHAINING OFF 
GO
ALTER DATABASE [leontest1] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [leontest1] SET TARGET_RECOVERY_TIME = 0 SECONDS 
GO
ALTER DATABASE [leontest1] SET DELAYED_DURABILITY = DISABLED 
GO
EXEC sys.sp_db_vardecimal_storage_format N'leontest1', N'ON'
GO
USE [leontest1]
GO
/****** Object:  Table [dbo].[leon1Test]    Script Date: 4/20/2023 2:17:03 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[leon1Test](
	[PersonID] [int] NULL,
	[LastName] [varchar](255) NULL,
	[FirstName] [varchar](255) NULL,
	[Address] [varchar](255) NULL,
	[City] [varchar](255) NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[usertesting]    Script Date: 4/20/2023 2:17:03 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[usertesting](
	[userId] [int] IDENTITY(1,1) NOT NULL,
	[name] [varchar](50) NULL,
	[email] [varchar](50) NULL,
	[age] [int] NULL,
	[contrasena] [varchar](50) NULL,
 CONSTRAINT [PK_usertesting] PRIMARY KEY CLUSTERED 
(
	[userId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
INSERT [dbo].[leon1Test] ([PersonID], [LastName], [FirstName], [Address], [City]) VALUES (1, N'leon', N'sebastian', N'juansebastianleon@gmail.com', N'cucuta')
GO
SET IDENTITY_INSERT [dbo].[usertesting] ON 

INSERT [dbo].[usertesting] ([userId], [name], [email], [age], [contrasena]) VALUES (1, N'daniela leon', N'juanandres@gamil.com', 27, NULL)
INSERT [dbo].[usertesting] ([userId], [name], [email], [age], [contrasena]) VALUES (2, N'german', N'juansebastianleon5@gmail.com', 45, NULL)
INSERT [dbo].[usertesting] ([userId], [name], [email], [age], [contrasena]) VALUES (3, N'login user', N'login.userURL@gmail.com', 1, NULL)
INSERT [dbo].[usertesting] ([userId], [name], [email], [age], [contrasena]) VALUES (4, N'adminsb', N'juansebastianleon5@gmail.com', 26, N'abcd1234')
INSERT [dbo].[usertesting] ([userId], [name], [email], [age], [contrasena]) VALUES (5, N'test', N'cocuyo5@hotmail.com', 11, N'abcd1234')
SET IDENTITY_INSERT [dbo].[usertesting] OFF
GO
USE [master]
GO
ALTER DATABASE [leontest1] SET  READ_WRITE 
GO

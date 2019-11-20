using System.Text;
using YoutuBot.Models;

namespace YoutuBot
{
    internal class QueryGenerator
    {
        public static string GenerateTableSchema()
        {
            return @"

CREATE TABLE [dbo].[Channels](
	[Id] [varchar](24) NOT NULL,
	[Name] [nvarchar](max) NULL,
	[LogoUrl] [nvarchar](max) NULL,
	[Keywords] [nvarchar](max) NULL,
	[Description] [nvarchar](max) NULL,
	[SubscriptionCount] [nvarchar](max) NULL,
	[Thumbnails] [nvarchar](max) NULL,
	[Tags] [nvarchar](max) NULL,
	[UserId] [nvarchar](max) NULL,
	[FromChannelId] [nvarchar](max) NULL,
 CONSTRAINT [PK_Channels] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[Comments](
	[Id] [varchar](100) NOT NULL,
	[Text] [nvarchar](max) NULL,
	[AuthorName] [nvarchar](max) NULL,
	[AuthorThumbnails] [nvarchar](max) NULL,
	[AuthorChannelId] [nvarchar](max) NULL,
	[PublishedTime] [nvarchar](max) NULL,
	[LikeCount] [nvarchar](max) NULL,
	[AuthorIsChannelOwner] [nvarchar](max) NULL,
	[ReplyCount] [nvarchar](max) NULL,
	[VideoId] [varchar](11) NULL,
	[Extra] [nvarchar](max) NULL,
	[CreatedDate] [datetime] NULL,
 CONSTRAINT [PK_Comments] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
CREATE TABLE [dbo].[Videos](
	[Id] [char](11) NOT NULL,
	[Title] [nvarchar](max) NULL,
	[Description] [nvarchar](max) NULL,
	[Length] [decimal](22, 6) NULL,
	[IsHD] [nvarchar](max) NULL,
	[CCLicense] [nvarchar](max) NULL,
	[IsCC] [nvarchar](max) NULL,
	[CommentsCount] [nvarchar](max) NULL,
	[ViewsCount] [nvarchar](max) NULL,
	[Rating] [nvarchar](max) NULL,
	[Keywords] [nvarchar](max) NULL,
	[LikesCount] [nvarchar](max) NULL,
	[CategoryId] [nvarchar](max) NULL,
	[DislikesCount] [nvarchar](max) NULL,
	[AddedOn] [nvarchar](max) NULL,
	[TimeCreated] [nvarchar](max) NULL,
	[UserId] [nvarchar](max) NULL,
	[Thumbnail] [nvarchar](max) NULL,
	[Privacy] [nvarchar](max) NULL,
	[Duration] [nvarchar](max) NULL,
	[Author] [nvarchar](max) NULL,
	[PublishedTime] [nvarchar](max) NULL,
	[IsLiveContent] [nvarchar](max) NULL,
	[ChannelId] [char](24) NULL,
	[Thumbnails] [nvarchar](max) NULL,
	[RichThumbnail] [nvarchar](max) NULL,
	[ChannelThumbnail] [nvarchar](max) NULL,
	[CreatedDate] [datetime] NULL,
 CONSTRAINT [PK_Videos] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [dbo].[Comments] ADD  CONSTRAINT [DF_Comments_CreatedDate]  DEFAULT (getutcdate()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[Videos] ADD  CONSTRAINT [DF_Videos_CreatedDate]  DEFAULT (getutcdate()) FOR [CreatedDate]
GO

";
        }

        public static string Generate(YoutubeChannelInfo channel)
        {
            var querySaveChannel = $@"
if not exists(select top(1) 1 from Channels where Id='{channel.Id}')
INSERT INTO [dbo].[Channels]
           ([Id]
           ,[Name]
           ,[LogoUrl]
           ,[Keywords]
           ,[Description]
           ,[SubscriptionCount]
           ,[Thumbnails]
           ,[Tags]
           ,[UserId]
           ,[FromChannelId])
     VALUES
           ('{channel.Id}'
           ,N'{channel.Name.Sqlize()}'
           ,N'{channel.LogoUrl.Sqlize()}'
           ,N'{channel.Keywords.Sqlize()}'
           ,N'{channel.Description.Sqlize()}'
           ,N'{channel.SubscriptionCount.Sqlize()}'
           ,N'{channel.Thumbnails.JoinWith().Sqlize()}'
           ,N'{channel.Tags.JoinWith().Sqlize()}'
           ,N'{channel.UserId.Sqlize()}'
           ,N'{channel.FromChannelId.Sqlize()}')
else
UPDATE [dbo].[Channels]
   SET 
       [Name] = N'{channel.Name.Sqlize()}'
      ,[LogoUrl] = N'{channel.LogoUrl.Sqlize()}'
      ,[Keywords] = N'{channel.Keywords.Sqlize()}'
      ,[Description] = N'{channel.Description.Sqlize()}'
      ,[SubscriptionCount] = N'{channel.SubscriptionCount.Sqlize()}'
      ,[Thumbnails] = N'{channel.Thumbnails.JoinWith().Sqlize()}'
      ,[Tags] = N'{channel.Tags.JoinWith().Sqlize()}'
      ,[UserId] = N'{channel.UserId.Sqlize()}'
      ,[FromChannelId] =ISNULL(FromChannelId,'')+',{channel.FromChannelId.Sqlize()}'
 WHERE [Id] = '{channel.Id}';
";
            var sb = new StringBuilder();
            sb.AppendLine(querySaveChannel);
            if (channel.Uploads != null)
                foreach (var videoInfo in channel.Uploads)
                {
                    var query = Generate(videoInfo);
                    sb.AppendLine(query);
                }

            return sb.ToString();
        }

        public static string Generate(YoutubeVideoInfo video)
        {
            if (string.IsNullOrEmpty(video.Id)) return string.Empty;

            return $@"
if NOT EXISTS(select TOP(1) 1 from Videos where Id='{video.Id.Sqlize()}')
INSERT INTO [dbo].[Videos]
           ([Id]
           ,[Title]
           ,[Description]
           ,[Length]
           ,[IsHD]
           ,[CCLicense]
           ,[IsCC]
           ,[CommentsCount]
           ,[ViewsCount]
           ,[Rating]
           ,[Keywords]
           ,[LikesCount]
           ,[CategoryId]
           ,[DislikesCount]
           ,[AddedOn]
           ,[TimeCreated]
           ,[UserId]
           ,[Thumbnail]
           ,[Privacy]
           ,[Duration]
           ,[Author]
           ,[PublishedTime]
           ,[IsLiveContent]
           ,[ChannelId]
           ,[Thumbnails]
           ,[RichThumbnail]
           ,[ChannelThumbnail])
     VALUES
           ('{video.Id.Sqlize()}'
           ,N'{video.Title.Sqlize()}'
           ,N'{video.Description.Sqlize()}'
           ,N'{video.Length.Sqlize()}'
           ,N'{video.IsHD.Sqlize()}'
           ,N'{video.CCLicense.Sqlize()}'
           ,N'{video.IsCC.Sqlize()}'
           ,N'{video.CommentsCount.Sqlize()}'
           ,N'{video.ViewsCount.Sqlize()}'
           ,N'{video.Rating.Sqlize()}'
           ,N'{video.Keywords.Sqlize()}'
           ,N'{video.LikesCount.Sqlize()}'
           ,N'{video.CategoryId.Sqlize()}'
           ,N'{video.DislikesCount.Sqlize()}'
           ,N'{video.AddedOn.Sqlize()}'
           ,N'{video.TimeCreated.Sqlize()}'
           ,N'{video.UserId.Sqlize()}'
           ,N'{video.Thumbnail.Sqlize()}'
           ,N'{video.Privacy.Sqlize()}'
           ,N'{video.Duration.Sqlize()}'
           ,N'{video.Author.Sqlize()}'
           ,N'{video.PublishedTime.Sqlize()}'
           ,N'{video.IsLiveContent.Sqlize()}'
           ,N'{video.ChannelId.Sqlize()}'
           ,N'{video.Thumbnails.JoinWith().Sqlize()}'
           ,N'{video.RichThumbnail.Sqlize()}'
           ,N'{video.ChannelThumbnail.JoinWith().Sqlize()}')


else 
UPDATE [dbo].[Videos]
   SET  
       [Title] = N'{video.Title.Sqlize()}'
      ,[Description] = N'{video.Description.Sqlize()}'
      ,[Length] = N'{video.Length.Sqlize()}'
      ,[IsHD] =N'{video.IsHD.Sqlize()}'
      ,[CCLicense] =N'{video.CCLicense.Sqlize()}'
      ,[IsCC] = N'{video.IsCC.Sqlize()}'
      ,[CommentsCount] = N'{video.CommentsCount.Sqlize()}'
      ,[ViewsCount] = N'{video.ViewsCount.Sqlize()}'
      ,[Rating] = N'{video.Rating.Sqlize()}'
      ,[Keywords] = N'{video.Keywords.Sqlize()}'
      ,[LikesCount] = N'{video.LikesCount.Sqlize()}'
      ,[CategoryId] = N'{video.CategoryId.Sqlize()}'
      ,[DislikesCount] =N'{video.DislikesCount.Sqlize()}'
      ,[AddedOn] = N'{video.AddedOn.Sqlize()}'
      ,[TimeCreated] = N'{video.TimeCreated.Sqlize()}'
      ,[UserId] = N'{video.UserId.Sqlize()}'
      ,[Thumbnail] = N'{video.Thumbnail.Sqlize()}'
      ,[Privacy] = N'{video.Privacy.Sqlize()}'
      ,[Duration] = N'{video.Duration.Sqlize()}'
      ,[Author] = N'{video.Author.Sqlize()}'
      ,[PublishedTime] = N'{video.PublishedTime.Sqlize()}'
      ,[IsLiveContent] = N'{video.IsLiveContent.Sqlize()}'
      ,[ChannelId] = N'{video.ChannelId.Sqlize()}'
      ,[Thumbnails] = N'{video.Thumbnails.JoinWith().Sqlize()}'
      ,[RichThumbnail] =N'{video.RichThumbnail.JoinWith().Sqlize()}'
      ,[ChannelThumbnail] = N'{video.ChannelThumbnail.JoinWith().Sqlize()}'
 WHERE Id='{video.Id.Sqlize()}';
 ";
        }

        public static string Generate(YoutubeVideoCommentInfo comment)
        {
            if (string.IsNullOrEmpty(comment.VideoId)) ;
            return $@"

if(not exists(select top(1) 1 from [dbo].[Comments] where Id=N'{comment.Id.Sqlize()}'))
INSERT INTO [dbo].[Comments]
           ([Id]
           ,[Text]
           ,[AuthorName]
           ,[AuthorThumbnails]
           ,[AuthorChannelId]
           ,[PublishedTime]
           ,[LikeCount]
           ,[AuthorIsChannelOwner]
           ,[VideoId]
           ,[Extra]
           ,[ReplyCount])
     VALUES
           (N'{comment.Id.Sqlize()}'
           ,N'{comment.Text.Sqlize()}'
           ,N'{comment.AuthorName.Sqlize()}'
           ,N'{comment.AuthorThumbnails.JoinWith().Sqlize()}'
           ,N'{comment.AuthorChannelId.Sqlize()}'
           ,N'{comment.PublishedTime.Sqlize()}'
           ,N'{comment.LikeCount.Sqlize()}'
           ,N'{comment.AuthorIsChannelOwner.Sqlize()}'
           ,N'{comment.VideoId.Sqlize()}'
           ,N'{comment.Extra.Sqlize()}'
           ,N'{comment.ReplyCount.Sqlize()}');
else 
UPDATE [dbo].[Comments]
   SET  
       [Text] = N'{comment.Text.Sqlize()}'
      ,[AuthorName] = N'{comment.AuthorName.Sqlize()}'
      ,[AuthorThumbnails] = N'{comment.AuthorThumbnails.JoinWith().Sqlize()}'
      ,[AuthorChannelId] = N'{comment.AuthorChannelId.Sqlize()}'
      ,[PublishedTime] = N'{comment.PublishedTime.Sqlize()}'
      ,[LikeCount] = N'{comment.LikeCount.Sqlize()}'
      ,[AuthorIsChannelOwner] = N'{comment.AuthorIsChannelOwner.Sqlize()}'
      ,[ReplyCount] = N'{comment.ReplyCount.Sqlize()}'
      ,[VideoId] = N'{comment.VideoId.Sqlize()}'
      ,[Extra] = N'{comment.Extra.Sqlize()}'
 WHERE Id=N'{comment.Id.Sqlize()}';


";
        }
    }
}
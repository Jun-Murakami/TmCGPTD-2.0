using Postgrest.Attributes;
using Postgrest.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TmCGPTD.Models
{
    public class PostageSqlModels
    {
        // チャットルーム-----------------------------------------------------------------------------------------------------------------
        [Table("chatrooms")]
        public class ChatRoom : BaseModel
        {
            [PrimaryKey("id")]
            public long Id { get; set; }

            [Column("user_id")]
            public string? UserId { get; set; }

            [Column("updated_on")]
            public DateTime UpdatedOn { get; set; }

            [Column("title")]
            public string? Title { get; set; }

            [Column("category")]
            public string? Category { get; set; }

            [Column("last_prompt")]
            public string? LastPrompt { get; set; }

            [Column("json")]
            public string? Json { get; set; }

            [Column("json_prev")]
            public string? JsonPrev { get; set; }

            public override bool Equals(object? obj)
            {
                return obj is ChatRoom chatroom &&
                        Id == chatroom.Id;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Id);
            }
        }

        // メッセージ-----------------------------------------------------------------------------------------------------------------
        [Table("messages")]
        public class Message : BaseModel
        {
            [PrimaryKey("id")]
            public long Id { get; set; }

            [Column("user_id")]
            public string? UserId { get; set; }

            [Column("room_id")]
            public long? RoomId { get; set; }

            [Column("created_on")]
            public DateTime CreatedOn { get; set; }

            [Column("content")]
            public string? Content { get; set; }

            [Column("role")]
            public string? Role { get; set; }

            [Column("usage")]
            public string? Usage { get; set; }

            public override bool Equals(object? obj)
            {
                return obj is Message message &&
                        Id == message.Id;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Id);
            }
        }

        // 送信履歴-----------------------------------------------------------------------------------------------------------------
        [Table("editorlogs")]
        public class EditorLog : BaseModel
        {
            [PrimaryKey("id")]
            public long Id { get; set; }

            [Column("user_id")]
            public string? UserId { get; set; }

            [Column("date")]
            public DateTime Date { get; set; }

            [Column("content")]
            public string? Content { get; set; }

            public override bool Equals(object? obj)
            {
                return obj is EditorLog editorlog &&
                        Id == editorlog.Id;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Id);
            }
        }

        // フレーズ-----------------------------------------------------------------------------------------------------------------
        [Table("phrases")]
        public class Phrase : BaseModel
        {
            [PrimaryKey("id")]
            public long Id { get; set; }

            [Column("user_id")]
            public string? UserId { get; set; }

            [Column("name")]
            public string? Name { get; set; }

            [Column("phrase")]
            public string? Content { get; set; }

            [Column("date")]
            public DateTime Date { get; set; }

            public override bool Equals(object? obj)
            {
                return obj is Phrase phrase &&
                        Id == phrase.Id;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Id);
            }
        }

        // テンプレート-----------------------------------------------------------------------------------------------------------------
        [Table("templates")]
        public class Template : BaseModel
        {
            [PrimaryKey("id")]
            public long Id { get; set; }

            [Column("user_id")]
            public string? UserId { get; set; }

            [Column("title")]
            public string? Title { get; set; }

            [Column("content")]
            public string? Content { get; set; }

            [Column("date")]
            public DateTime Date { get; set; }

            public override bool Equals(object? obj)
            {
                return obj is Template templete &&
                        Id == templete.Id;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Id);
            }
        }
        // 削除フラグ管理用-----------------------------------------------------------------------------------------------------------------
        [Table("management")]
        public class Management : BaseModel
        {
            [PrimaryKey("id")]
            public long Id { get; set; }

            [Column("user_id")]
            public string? UserId { get; set; }

            [Column("delete_table")]
            public string? DeleteTable { get; set; }

            [Column("delete_id")]
            public long DeleteId { get; set; }

            [Column("date")]
            public DateTime Date { get; set; }

            public override bool Equals(object? obj)
            {
                return obj is Template management &&
                        Id == management.Id;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Id);
            }
        }
    }
}

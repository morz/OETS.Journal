using System;
using System.Collections.Generic;

namespace OETS.Shared.Opcodes
{
    /// <summary>
    /// КОМАНДЫ
    /// </summary>
    public enum OpcoDes : int
    {
        CMSG_REQUEST_USER_LOGIN,

        CMSG_GETTING_JOURNAL,

        CMSG_GETTING_JOURNAL_2,

        CMSG_SEND_JOURNAL_ENTRY,
        /// <summary>
        /// СЕРВЕР - Сообщение об ошибке
        /// </summary>
        SMSG_ERROR,
        /// <summary>
        /// КЛИЕНТ - Запрос на пинг
        /// </summary>
        CMSG_PONG,
        /// <summary>
        /// СЕРВЕР - Ответ на пинг
        /// </summary>
        SMSG_PING,


        SMSG_USER_AUTHENTICATED,

        SMSG_JOURNAL_MODIFY,
        SMSG_JOURNAL_REMOVE,
        SMSG_JOURNAL_ADD,

        SMSG_SERVER_STOPED,
        SMSG_SERVER_DISCONNECTED,

        MAX_NUM
    }
}

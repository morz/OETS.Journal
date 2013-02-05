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
        CMSG_TEST,
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

        SMSG_SERVER_DISCONNECTED,

        MAX_NUM
    }
}

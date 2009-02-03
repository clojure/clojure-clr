#region Copyright(c) 2006 ZO, All right reserved.
// -----------------------------------------------------------------------------
// Copyright(c) 2006 ZO, All right reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//
//   1.  Redistribution of source code must retain the above copyright notice,
//       this list of conditions and the following disclaimer.
//   2.  Redistribution in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//   3.  The name of the author may not be used to endorse or promote products
//       derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR IMPLIED
// WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
// MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO
// EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
// PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS;
// OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
// WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
// OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
// ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// -----------------------------------------------------------------------------
#endregion


#region Using directives
using System;
using System.Runtime.Serialization;
using System.Security.Permissions;
using ZO.SmartCore.Helpers;
#endregion

namespace ZO.SmartCore.Core
{
    /// <summary>
    ///  The exception that is thrown when <see langword="null" /> is passed to a 
    ///  method that does not accept it as a valid argument. 
    /// </summary>
    /// <seealso cref="T:System.Exception"/>
    /// <remarks>
    /// <see cref="ArgumentNullException"/> is thrown when a method is 
    /// invoked and at least one of the passed arguments is <see langword="null" /> but 
    /// should never be <see langword="null" />.
    /// </remarks>
    /// <threadsafety static="true" instance="false"/>
    [Serializable]
    public class ArgumentNullException : System.ArgumentNullException
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ArgumentNullException"/> class.
        /// </summary>
        /// <overloads>
        /// Initializes a new instance of the <see cref="ArgumentNullException"/> class.
        /// </overloads>
        public ArgumentNullException() : base() { }


        /// <summary>
        /// Initializes a new instance of the <see cref="ArgumentNullException"/> class with 
        /// a specified error message and a reference to the inner exception that is the cause 
        /// of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or <see langword="null" /> if no inner exception is specified.</param>
        public ArgumentNullException(string message, Exception innerException) : base(message, innerException) { }


        /// <summary>
        /// Initializes a new instance of the <see cref="ArgumentNullException"/> class 
        /// with the name of the parameter that causes this exception.
        /// </summary>
        /// <param name="paramName">The name of the parameter that caused the exception.</param>
        public ArgumentNullException(string paramName) : base(paramName) { }


        /// <summary>
        /// Initializes a new instance of the <see cref="ArgumentNullException"/> class with a 
        /// specified error message and the name of the parameter that causes this exception.
        /// </summary>
        /// <param name="paramName">The name of the parameter that caused the current exception.</param>
        /// <param name="message">The error message that explains the reason for the exception. .</param>
        public ArgumentNullException(string paramName, string message) : base(paramName, message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArgumentNullException"/> class with a specified error message, the name of the parameter that causes this exception and  objects to format the message.
        /// </summary>
        /// <param name="paramName">The name of the parameter that caused the current exception.</param>
        /// <param name="message">The error message that explains the reason for the exception. .</param>
        /// <param name="args">An Object array containing zero or more objects to format.</param>
        public ArgumentNullException(string paramName, string message, params object[] args) : base(paramName, StringHelper.Format(message, args)) { }


        /// <summary>
        /// Initializes a new instance of the <see cref="ArgumentNullException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">An object that describes the source or destination of the serialized data.</param>
        protected ArgumentNullException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        #endregion

        #region Methods
        /// <summary>
        /// Sets the <see cref="T:System.Runtime.Serialization.SerializationInfo"></see> object with the parameter name and additional exception information.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        /// <exception cref="T:System.ArgumentNullException">The info object is a null reference (Nothing in Visual Basic). </exception>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }


        #endregion

    }

}

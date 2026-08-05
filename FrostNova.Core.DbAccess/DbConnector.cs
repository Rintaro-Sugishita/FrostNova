using System;
using System.Collections.Generic;
using System.Data;

namespace FrostNova.Core.DbAccess
{
    /// <summary>
    /// 任意の ADO.NET <see cref="IDbConnection"/> をラップした読み取り専用の軽量コネクタ。
    /// SQL を実行し結果を POCO のリストへマッピングする用途に特化する。
    /// 旧 DBUtil.CommonConnector から、接続 + クエリ + マッピングのみを抽出して再設計
    /// （トランザクション / 属性 ORM / DbProviderFactory / 例外握り潰し を排除、例外はそのまま伝播）。
    /// </summary>
    public sealed class DbConnector : IDisposable
    {
        private readonly IDbConnection _connection;

        public DbConnector(IDbConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public ConnectionState State => _connection.State;

        /// <summary>接続を開く（失敗時は例外を伝播）。</summary>
        public void Connect()
        {
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }
        }

        /// <summary>接続を閉じる。</summary>
        public void Close()
        {
            if (_connection.State != ConnectionState.Closed)
            {
                _connection.Close();
            }
        }

        /// <summary>開いて閉じるだけの接続テスト。成功可否を返す。</summary>
        public bool ConnectTest()
        {
            try
            {
                _connection.Open();
                _connection.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// SQL を実行し、結果の各行を <typeparamref name="T"/> にマッピングして返す。
        /// 接続が未オープンなら自動で開く（Dispose まで開いたまま）。
        /// </summary>
        public List<T> Query<T>(string sql, IEnumerable<IDbDataParameter>? parameters = null) where T : new()
        {
            Connect();

            using (var command = _connection.CreateCommand())
            {
                command.CommandType = CommandType.Text;
                command.CommandText = sql;

                if (parameters != null)
                {
                    foreach (var p in parameters)
                    {
                        command.Parameters.Add(p);
                    }
                }

                using (var reader = command.ExecuteReader())
                {
                    var table = new DataTable();
                    table.Load(reader);
                    return DbRowMapper.Map<T>(table);
                }
            }
        }

        /// <summary>パラメータを生成するヘルパ（null は DBNull に変換）。</summary>
        public IDbDataParameter CreateParameter(string name, object? value)
        {
            using (var command = _connection.CreateCommand())
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = name;
                parameter.Value = value ?? DBNull.Value;
                return parameter;
            }
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}

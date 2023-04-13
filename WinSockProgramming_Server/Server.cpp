#include "Server.h"

int main() {

	// ポート番号
	int port_number = 12345;

	// Windows Sockets仕様に関する情報を格納する構造体
	WSADATA wsa_data;

	// WinSockの初期化処理(Version 2.0)
	if (WSAStartup(MAKEWORD(2, 0), &wsa_data) != 0) {
		std::cerr << "Winsockの初期化失敗(WSAStartup)" << std::endl;
	}

	// サーバ側ソケット作成
	int src_socket;

	// sockaddr_in構造体の作成とポート番号、IPタイプの入力
	struct sockaddr_in src_addr;
	memset(&src_addr, 0, sizeof(src_addr));
	src_addr.sin_port = htons(port_number);
	src_addr.sin_family = AF_INET;
	src_addr.sin_addr.s_addr = htonl(INADDR_ANY);

	// AF_INETはipv4のIPプロトコル & SOCK_STREAMはTCPプロトコル
	src_socket = socket(AF_INET, SOCK_STREAM, 0);

	// サーバ側のソケットを特定のIPアドレスとポートに紐付ける
	bind(src_socket, (struct sockaddr*)&src_addr, sizeof(src_addr));

	// クライアント側のソケット設定
	int dst_socket;
	struct sockaddr_in dst_addr;
	int dst_addr_size = sizeof(dst_addr);

	// 接続の待受を開始する
	listen(src_socket, 1);

	// 送受信に使用するバッファ
	char recv_buf1[256], recv_buf2[256];
	char send_buf[256];

	// クライアントからの接続待ちループ関数
	while (1) {

		std::cout << "クライアントからの接続待ち" << std::endl;

		// クライアントからの接続を受信する
		dst_socket = accept(src_socket, (struct sockaddr*)&dst_addr, &dst_addr_size);

		std::cout << "クライアントからの接続有り" << std::endl;

		// 接続後の処理
		while (1) {

			int status;

			//パケットの受信(recvは成功すると受信したデータのバイト数を返却。切断で0、失敗で-1が返却される
			int recv1_result = recv(dst_socket, recv_buf1, sizeof(char) * 256, 0);
			if (recv1_result == 0 || recv1_result == -1) {
				status = closesocket(dst_socket); break;
			}
			std::cout << "受信した数字1個目 : " << recv_buf1 << std::endl;

			int recv2_result = recv(dst_socket, recv_buf2, sizeof(char) * 256, 0);
			if (recv2_result == 0 || recv2_result == -1) {
				status = closesocket(dst_socket); break;
			}
			std::cout << "受信した数字2個目 : " << recv_buf2 << std::endl;

			// 受信した数字を加算
			int sum = atoi(recv_buf1) + atoi(recv_buf2);
			snprintf(send_buf, 256, "%d", sum);
			std::cout << "結果 : " << atoi(recv_buf1) << "+" << atoi(recv_buf2) << "=" << sum << std::endl;

			// 結果を格納したパケットの送信
			send(dst_socket, send_buf, sizeof(char) * 256, 0);
		}
	}

	// WinSockの終了処理
	WSACleanup();

	return 0;
}
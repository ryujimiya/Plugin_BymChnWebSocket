/**
 * �ڑ���
 */
const HOST = 'localhost';
//const PORT = 50001;  // �_�ǂ݂���񒼐�
const PORT = 50002;  // WebSocket�T�[�o�[�o�R

/**
 * �R���X�g���N�^
 */
const BouyomiChanClient = function() {
}

/**
 * �ǂݏグ��
 */
BouyomiChanClient.prototype.talk = function(cmntStr) {
    this.cmntStr = cmntStr;
    let _socket = new WebSocket('ws://' + HOST + ':' + PORT + '/ws/');
    this.socket = _socket;
    this.socket.binaryType = 'arraybuffer';
    this.socket.onopen = this.socket_onopen.bind(this);
    this.socket.onerror = this.socket_onerror.bind(this);
    this.socket.onclose = this.socket_onclose.bind(this);
    this.socket.onmessage = this.socket_onmessage.bind(this);
}

/**
 * WebSocket���ڑ�����
 */
BouyomiChanClient.prototype.socket_onopen = function(e) {
    console.log("socket_onopen");
    
    // �_�ǂ݂����f�[�^�𐶐�
    let data = this.makeBouyomiChanDataToSend(this.cmntStr);
    console.log(data);
    // ���M
    this.socket.send(data.buffer);
}


/**
 * WebSocket���ڑ��Ɏ��s����
 */
BouyomiChanClient.prototype.socket_onerror = function(e) {
    console.log("socket_onerror");

    this.socket.close();
}

/**
 * WebSocket���N���[�Y����
 */
BouyomiChanClient.prototype.socket_onclose = function(e) {
    console.log("socket_onclose");
}

/**
 * WebSocket���f�[�^����M����
 */
BouyomiChanClient.prototype.socket_onmessage = function(e) {
    console.log("socket_onmessage");
    console.log(e.data);

    // �f�[�^��M������ؒf����
    this.socket.close();
}


/**
 * �_�ǂ݂����֑��M����f�[�^�̐���
 */
BouyomiChanClient.prototype.makeBouyomiChanDataToSend = function(cmntStr) {
    let command = 0x0001; //[0-1]  (16Bit) �R�}���h          �i 0:���b�Z�[�W�ǂݏグ�j
    let speed = -1; //[2-3]  (16Bit) ���x              �i-1:�_�ǂ݂�����ʏ�̐ݒ�j
    let tone = -1; //[4-5]  (16Bit) ����              �i-1:�_�ǂ݂�����ʏ�̐ݒ�j
    let volume = -1; //[6-7]  (16Bit) ����              �i-1:�_�ǂ݂�����ʏ�̐ݒ�j
    let voice = 0; //[8-9]  (16Bit) ����              �i 0:�_�ǂ݂�����ʏ�̐ݒ�A1:����1�A2:����2�A3:�j��1�A4:�j��2�A5:�����A6:���{�b�g�A7:�@�B1�A8:�@�B2�A10001�`:SAPI5�j
    let code = 0; //[10]   ( 8Bit) ������̕����R�[�h�i 0:UTF-8, 1:Unicode, 2:Shift-JIS�j
    let len = 0; //[11-14](32Bit) ������̒���

    let cmntByteArray = stringToUtf8ByteArray(cmntStr);
    
    len = cmntByteArray.length;
    let bytesLen = 2 + 2 + 2 + 2 + 2 + 1 + 4 + cmntByteArray.length;
    let data = new Uint8Array(bytesLen);
    let pos = 0;
    data[pos++] = command & 0xFF;
    data[pos++] = (command >> 8) & 0xFF;
    data[pos++] = speed & 0xFF;
    data[pos++] = (speed >> 8) & 0xFF;
    data[pos++] = tone & 0xFF;
    data[pos++] = (tone >> 8) & 0xFF;
    data[pos++] = volume & 0xFF;
    data[pos++] = (volume >> 8) & 0xFF;
    data[pos++] = voice & 0xFF;
    data[pos++] = (voice >> 8) & 0xFF;
    data[pos++] = code & 0xFF;
    data[pos++] = len & 0xFF;
    data[pos++] = (len >> 8) & 0xFF;
    data[pos++] = (len >> 16) & 0xFF;
    data[pos++] = (len >> 24) & 0xFF;
    for (let i = 0; i < cmntByteArray.length; i++) {
        data[pos++] = cmntByteArray[i];
    }
    return data;
}

///////////////////////////////////////////////////////////////////////////////////////
// Util
/**
 * string --> UTF8 byteArray�ϊ�
 */
function stringToUtf8ByteArray(str) {
    let out = [], p = 0;
    for (var i = 0; i < str.length; i++) {
        let c = str.charCodeAt(i);
        if (c < 128) {
            out[p++] = c;
        }
        else if (c < 2048) {
            out[p++] = (c >> 6) | 192;
            out[p++] = (c & 63) | 128;
        }
        else if (
            ((c & 0xFC00) == 0xD800) && (i + 1) < str.length &&
            ((str.charCodeAt(i + 1) & 0xFC00) == 0xDC00)) {

            // Surrogate Pair
            c = 0x10000 + ((c & 0x03FF) << 10) + (str.charCodeAt(++i) & 0x03FF);
            out[p++] = (c >> 18) | 240;
            out[p++] = ((c >> 12) & 63) | 128;
            out[p++] = ((c >> 6) & 63) | 128;
            out[p++] = (c & 63) | 128;
        }
        else {
            out[p++] = (c >> 12) | 224;
            out[p++] = ((c >> 6) & 63) | 128;
            out[p++] = (c & 63) | 128;
        }
    }
    return out;
}



import json

from flask import Flask, Response, jsonify, request
import pymongo

app = Flask(__name__)

user = "agost"
password = "agost"

@app.route('/coffee', methods=['GET'])
def test():
    print("test")
    return jsonify({"code": 200})

@app.route('/getmanifest', methods=['GET'])
def mongoget():
    client = pymongo.MongoClient("mongodb+srv://{}:{}@cluster0.vhjvg.mongodb.net/?retryWrites=true&w=majority".format(user, password))
    db = client.test
    collection  = db.remoteAuthoring
    try:
        doc = collection.find_one({"dataKey": "manifest_json"})
        # print(doc["manifest"])
        response = doc["manifest"]
    except Exception as e:
        print(e)
        return {}
        # response = {"Exception": e}
    print(jsonify(response))
    return jsonify(response)


if __name__ == "__main__":
    server_host = '0.0.0.0'
    server_port = '5008'
    app.run(host=server_host, port=server_port, debug=True)
